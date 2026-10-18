import numpy as np, sys
from PIL import Image
from scipy import ndimage as ndi
S='/workspace/wt-old-v/Assets/_Project/Art/Resources/Sprites/'
def load(c):
    return Image.open('real_V.png' if c=='V' else S+'fx_let_%s.png'%c).convert('RGBA')
def near(a,col,t=12): return (np.abs(a[...,:3]-np.array(col)).sum(-1)<t)&(a[...,3]>200)
def stats(im):
    a=np.asarray(im).astype(int)
    al=a[...,3]>128
    yel=al&(a[...,0]>=100)
    dark=al&(a[...,0]<100)
    out=dark&(a[...,2]>=30); sh=dark&(a[...,2]<30)
    rim=al&(a[...,1]>=232)&(a[...,2]>=130)
    g=ndi.gaussian_filter(yel.astype(float),3); gy,gx=np.gradient(g)
    edge=yel&~ndi.binary_erosion(yel)
    ys,xs=np.nonzero(edge)
    n=np.hypot(gx,gy)[ys,xs]+1e-9; nx=-gx[ys,xs]/n; ny=-gy[ys,xs]/n
    th=ndi.distance_transform_edt(out|yel)
    t=th[ys,xs]
    res={}
    for k,m in {'L':nx<-.8,'R':nx>.8,'T':ny<-.8,'B':ny>.8}.items():
        res[k]=round(float(np.median(t[m])),1) if m.sum()>20 else None
    rd=ndi.distance_transform_edt(yel)
    res['rimW']=round(float(np.percentile(rd[rim],95)),1) if rim.sum() else 0
    ob=np.nonzero(al.any(0))[0]; oy=np.nonzero(al.any(1))[0]
    res['shadowR']=int(ob[-1]-np.nonzero((out|yel).any(0))[0][-1]); res['shadowB']=int(oy[-1]-np.nonzero((out|yel).any(1))[0][-1])
    res['marginL']=int(ob[0]); res['marginT']=int(oy[0]); res['size']=im.size
    return res
if __name__=='__main__':
    for c in 'FLOCKIVE':
        try: print(c,stats(load(c)))
        except Exception as e: print(c,'ERR',e)
