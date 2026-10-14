import json,sys,os; sys.path.insert(0,'.')
from smooth import *; from inventory import T
SRC={'zfx-jingle v4.1 (shipped ogg)':'/workspace/ff/b83/zfx-jingle/zfx-jingle.wav'}
LOOPS={'splash-theme','garden-theme','badger-battle'}
before=json.load(open('../before.json')); info={n:(p,g,fm,nm,cl,tg,dst) for n,p,g,fm,nm,cl,tg,dst in T}
os.makedirs('../after/assets',exist_ok=True)
def dither16(y):
    rng=np.random.default_rng(7); r=(rng.random(y.shape)-rng.random(y.shape))/32768
    return np.clip(np.round((y+r)*32767),-32768,32767).astype(np.int16)
def raw(n):
    x=load(SRC.get(n,info[n][0])); return np.repeat(x,2,1) if x.shape[1]==1 else x
def write(n,y):
    dst=info[n][6]; fn='../after/assets/'+os.path.basename(dst)
    if fn.endswith('.ogg'):
        sf.write(fn[:-4]+'.wav',dither16(y),SR,subtype='PCM_16'); os.system(f'ffmpeg -y -loglevel error -i {fn[:-4]}.wav -c:a libvorbis -q:a 8 {fn}')
    else: sf.write(fn,dither16(y),SR,subtype='PCM_16')
    return load(fn)
def proc(n,gain=None):
    x=raw(n); tg=info[n][5]
    if n in LOOPS:
        pad=int(3*SR); xx=np.concatenate([x[-pad:],x])
    else: xx=x; pad=0
    y,ds,tr=polish(xx); y=y[pad:]
    if gain is None: gain=10**((tg-lufs(y))/20)
    y=y*gain
    for _ in range(4):
        if true_peak(y)<=-1.5: break
        y=limit(np.concatenate([y[-pad:],y]) if pad else y)[pad:]
    return y,gain,ds,tr
def bgame(n): return before[n]['lufs']+20*np.log10(info[n][1])
res={}; caps={}
def solve_cap(names,trim,clipmax):
    ys={n:write(n,proc_out[n]) for n in names}
    want=np.mean([bgame(n) for n in names])+trim
    cap=min(1.0,10**((want-np.mean([lufs(ys[n]) for n in names]))/20))
    floor=cap*10**(-2.0/20)
    while max(phone(ys[n],cap)[1] for n in names)>clipmax and cap>floor: cap*=10**(-0.25/20)
    for n in names: caps[n]=(info[n][1],round(float(cap),3))
    return ys,cap
proc_out={}; meta={}
for n in ['splash-theme','garden-theme','badger-battle','badger-win','badger-lose','zfx-jingle v4.1 (shipped ogg)']:
    y,g,ds,tr=proc(n); proc_out[n]=y; meta[n]=(ds,tr,g)
# entrance is spliced bar-for-bar into the battle loop on the same BadgerCap: same static gain as the loop
y,g,ds,tr=proc('badger-entrance',gain=meta['badger-battle'][2]); proc_out['badger-entrance']=y; meta['badger-entrance']=(ds,tr,g)
groups=[(['splash-theme'],-1.0,1.0),(['garden-theme'],-1.0,1.0),(['badger-battle','badger-entrance'],-1.0,1.0),(['badger-win','badger-lose'],-1.5,1.5),(['zfx-jingle v4.1 (shipped ogg)'],-1.5,1.5)]
for names,trim,cm in groups:
    if names[0]=='badger-battle':
        ys={n:write(n,proc_out[n]) for n in names}; want=bgame('badger-battle')+trim
        cap=min(1.0,10**((want-lufs(ys['badger-battle']))/20))
        for n in names: caps[n]=(info[n][1],round(float(cap),3))
    else: ys,cap=solve_cap(names,trim,cm)
    for n in names:
        y=ys[n]; r=metrics(y,cap); r['ds'],r['tr'],_=meta[n]; r['game']=lufs(y)+20*np.log10(cap); r['bgame']=bgame(n); r['gain']=float(cap)
        r['mono_fold']=lufs(np.repeat(y.mean(1,keepdims=True),2,1))-lufs(y)
        r['seam']=float(np.abs(y[0]-y[-1]).max()) if n in LOOPS else None
        res[n]=r; b=before[n]
        print(f"{n:28s} cap {info[n][1]:.2f}->{cap:.3f} LUFS {b['lufs']:6.1f}->{r['lufs']:6.1f} TP {b['tp']:5.1f}->{r['tp']:5.1f} crest {b['crest']:4.1f}->{r['crest']:4.1f} e38 {b['e38']:4.2f}->{r['e38']:4.2f} sib {b['sib']:5.1f}->{r['sib']:5.1f} res {b['res_db']:3.1f}->{r['res_db']:3.1f} | ph harsh {b['ph_harsh']:5.1f}->{r['ph_harsh']:5.1f} clip {b['ph_clip']:5.2f}->{r['ph_clip']:5.2f} dist {b['ph_dist']:5.1f}->{r['ph_dist']:5.1f} | game {r['bgame']:.1f}->{r['game']:.1f} mono {r['mono_fold']:.2f} seam {r['seam']}")
json.dump(res,open('../after_assets.json','w'),indent=1); json.dump(caps,open('../caps.json','w'),indent=1)
