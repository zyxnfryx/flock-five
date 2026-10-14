"""Flock Five music smoothness pass: metrics, phone-speaker sim, remaster chain."""
import numpy as np, scipy.signal as ss, soundfile as sf, pyloudnorm as pyln
SR=44100
def load(p):
    x,sr=sf.read(p,always_2d=True,dtype='float64')
    if sr!=SR: x=ss.resample_poly(x,SR,sr,axis=0)
    return x
def as_imported(x,force_mono,normalize):
    """what Unity plays: ForceToMono sums L+R, Normalize peaks to 0 dBFS; mono plays on both speakers"""
    if force_mono and x.shape[1]>1: x=x.mean(1,keepdims=True)
    if normalize: x=x/np.abs(x).max()
    if x.shape[1]==1: x=np.repeat(x,2,1)
    return x
# ---------- RBJ biquads (same math as AudioPolish.cs) ----------
def shelf_hi(fc,gdb,S=0.7):
    A=10**(gdb/40);w=2*np.pi*fc/SR;c,s=np.cos(w),np.sin(w)
    al=s/2*np.sqrt((A+1/A)*(1/S-1)+2)
    b=[A*((A+1)+(A-1)*c+2*np.sqrt(A)*al),-2*A*((A-1)+(A+1)*c),A*((A+1)+(A-1)*c-2*np.sqrt(A)*al)]
    a=[(A+1)-(A-1)*c+2*np.sqrt(A)*al,2*((A-1)-(A+1)*c),(A+1)-(A-1)*c-2*np.sqrt(A)*al]
    return np.array(b)/a[0],np.array(a)/a[0]
def bpf(fc,Q):
    w=2*np.pi*fc/SR;al=np.sin(w)/(2*Q);c=np.cos(w)
    b=[al,0,-al];a=[1+al,-2*c,1-al];return np.array(b)/a[0],np.array(a)/a[0]
def peak(fc,gdb,Q):
    A=10**(gdb/40);w=2*np.pi*fc/SR;al=np.sin(w)/(2*Q);c=np.cos(w)
    b=[1+al*A,-2*c,1-al*A];a=[1+al/A,-2*c,1-al/A];return np.array(b)/a[0],np.array(a)/a[0]
def coef(ms): return 1-np.exp(-1/(SR*ms/1000))
def follow(e,att,rel):
    a1=1-np.exp(-1/(44100*att/1000));r1=1-np.exp(-1/(44100*rel/1000));y=np.empty_like(e);v=0.
    for i,x in enumerate(e):
        v+= (a1 if x>v else r1)*(x-v); y[i]=v
    return y
try:
    from numba import njit
    follow=njit(cache=True)(follow)
except Exception: pass
# ---------- remaster chain ----------
P=dict(shelf_fc=4800,shelf_db=-3.0, ds_fc=5200,ds_q=0.75,ds_thr_db=-14.0,ds_max_db=7.0,
       tr_ratio=1.5,tr_max_db=3.5, pr_fc=2400,pr_q=0.9,pr_thr_db=-20.0,pr_ratio=2.0,pr_max_db=4.0)
def polish(x,p=P):
    b,a=shelf_hi(p['shelf_fc'],p['shelf_db']); y=ss.lfilter(b,a,x,axis=0)
    # dynamic de-ess: band 3-8k vs full-band envelope, linked stereo
    b,a=bpf(p['ds_fc'],p['ds_q']); band=ss.lfilter(b,a,y,axis=0)
    eb=follow(np.abs(band).max(1),1.0,60.0); ef=follow(np.abs(y).max(1),1.0,60.0)
    thr=10**(p['ds_thr_db']/20)
    over=np.where(eb>thr*ef+1e-9,(thr*ef+1e-9)/(eb+1e-12),1.0)
    g=np.maximum(over,10**(-p['ds_max_db']/20))
    g=1-follow(1-g,0.5,40.0)
    y=y-band*(1-g)[:,None]
    # dynamic presence band (phone-speaker resonance zone ~2-3 kHz): absolute-threshold, only loud peaks
    b,a=bpf(p['pr_fc'],p['pr_q']); band=ss.lfilter(b,a,y,axis=0)
    ep=follow(np.abs(band).max(1),2.0,80.0); thr=10**(p['pr_thr_db']/20)
    gp=np.where(ep>thr,(thr/(ep+1e-12))**(1-1/p['pr_ratio']),1.0)
    gp=np.maximum(gp,10**(-p['pr_max_db']/20)); gp=1-follow(1-gp,1.0,60.0)
    y=y-band*(1-gp)[:,None]
    # transient softening: fast vs slow peak envelope
    e=np.abs(y).max(1); fast=follow(e,0.3,25.0); slow=follow(e,12.0,140.0)
    lim=slow*p['tr_ratio']
    gt=np.where(fast>lim,np.sqrt(lim/(fast+1e-12)),1.0)
    gt=np.maximum(gt,10**(-p['tr_max_db']/20))
    gt=1-follow(1-gt,0.4,30.0)
    return y*gt[:,None], 20*np.log10(g.min()), 20*np.log10(gt.min())
def true_peak(x): return 20*np.log10(np.abs(ss.resample_poly(x,4,1,axis=0)).max()+1e-12)
def lufs(x):
    m=pyln.Meter(SR,block_size=0.4 if len(x)/SR>0.5 else len(x)/SR*0.8)
    y=x if x.shape[1]>1 else x
    try: return m.integrated_loudness(y if y.shape[1]>1 else y[:,0])
    except Exception: return float('nan')
def limit(x,ceil_db=-1.6,look_ms=1.5,rel_ms=80):
    """lookahead peak limiter on 4x-oversampled envelope; then TP check"""
    c=10**(ceil_db/20); L=int(SR*look_ms/1000)
    up=np.abs(ss.resample_poly(x,4,1,axis=0)).max(1).reshape(-1,4).max(1)[:len(x)]
    need=np.minimum(1,c/(up+1e-12))
    # lookahead min over window then smooth release
    from scipy.ndimage import minimum_filter1d
    need=minimum_filter1d(need,2*L+1,origin=0)
    g=1-follow(1-need,0.0001,rel_ms)
    g=np.minimum(g,need)
    return x*g[:,None]
def master(x,target_lufs):
    y,ds,tr=polish(x)
    l=lufs(y); y=y*10**((target_lufs-l)/20)
    for _ in range(4):
        y=limit(y)
        if true_peak(y)<=-1.5: break
    l2=lufs(y)
    if abs(l2-target_lufs)>0.3 and l2<target_lufs: pass  # crest-limited track: keep TP
    return y,ds,tr
# ---------- metrics ----------
def bandE(x,lo,hi):
    f,P=ss.welch(x.mean(1),SR,nperseg=4096); m=(f>=lo)&(f<hi); return P[m].sum()/P.sum()
def sib(x):
    m=x.mean(1); b,a=ss.butter(4,[4000,9000],'band',fs=SR); hb=ss.lfilter(b,a,m)
    w=int(.05*SR); n=len(m)//w
    if n<2: return 0.
    r=[10*np.log10((hb[i*w:(i+1)*w]**2).sum()/((m[i*w:(i+1)*w]**2).sum()+1e-15)+1e-15) for i in range(n) if (m[i*w:(i+1)*w]**2).mean()>1e-7]
    return float(np.percentile(r,95)) if r else -99.
def _octsmooth(f,P,frac):
    out=np.empty_like(P)
    for i,fc in enumerate(f):
        lo,hi=fc*2**(-0.5/frac),fc*2**(0.5/frac); m=(f>=lo)&(f<=hi); out[i]=P[m].mean() if m.any() else P[i]
    return out
def reson(x):
    """harsh-resonance peak: 1/6-oct smoothed spectrum above its 1-oct trend, 2-8 kHz"""
    f,P=ss.welch(x.mean(1),SR,nperseg=4096); m=(f>=1500)&(f<9000)
    a=10*np.log10(_octsmooth(f,P,6)+1e-20); b=10*np.log10(_octsmooth(f,P,1)+1e-20)
    d=(a-b); k=np.argmax(np.where((f>=2000)&(f<8000),d,-99))
    return float(d[k]),float(f[k])
def crest(x): return 20*np.log10(np.abs(x).max()/np.sqrt((x**2).mean()))
def phone(x,play_gain,drive_db=14.0):
    """small-speaker sim at max volume: mono, BP 300-8k, +6dB @3k resonance, soft saturation"""
    m=x.mean(1)*play_gain*10**(drive_db/20)
    b,a=ss.butter(2,[300,8000],'band',fs=SR); m=ss.lfilter(b,a,m)
    b,a=peak(3000,6.0,1.1); m=ss.lfilter(b,a,m)
    clip=float((np.abs(m)>0.8).mean()*100)
    y=np.tanh(1.6*m)/np.tanh(1.6)
    return y[:,None],clip
def harsh(y):
    f,P=ss.welch(y.mean(1),SR,nperseg=4096)
    hi=P[(f>=2000)&(f<6000)].sum(); lo=P[(f>=300)&(f<2000)].sum()
    return 10*np.log10(hi/lo)
def thd_proxy(pre,post):
    """energy added by speaker saturation above 3 kHz (dB re signal)"""
    p=post[:,0]; k=(p@pre)/(pre@pre+1e-15); d=p-k*pre
    return 10*np.log10((d**2).sum()/((post**2).sum()+1e-15)+1e-15)
def metrics(x,play_gain):
    out=dict(lufs=lufs(x),tp=true_peak(x),crest=crest(x),e38=100*bandE(x,3000,8000),sib=sib(x))
    out['res_db'],out['res_hz']=reson(x)
    pre=x.mean(1)*play_gain*10**(14/20)
    b,a=ss.butter(2,[300,8000],'band',fs=SR); pre=ss.lfilter(b,a,pre); b,a=peak(3000,6.0,1.1); pre=ss.lfilter(b,a,pre)
    y,clip=phone(x,play_gain)
    out.update(ph_lufs=lufs(y),ph_harsh=harsh(y),ph_sib=sib(y),ph_clip=clip,ph_dist=thd_proxy(pre,y),ph_e38=100*bandE(y,3000,8000))
    return out
