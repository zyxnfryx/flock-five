"""LetterStyle: one normalization step for finale lettering.
Keeps each glyph's yellow shape and canvas; rebuilds rim (3.2px), outline (36px, round)
and drop shadow (+16,+16) so every letter has the F/I/E family styling."""
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
FILL=(248,224,96); RIM=(255,240,160); OUT=(8,14,36); SH=(5,9,24)
RIM_W=6.0; OUT_W=35.2; SH_DX=16; SH_DY=16; SS=3
def yellow_mask(a):
    r,g,b,al=[a[...,i].astype(int) for i in range(4)]
    return (al>128)&(r>200)&(g>170)&(b<190)
def normalize(im):
    a=np.asarray(im.convert('RGBA'))
    y=yellow_mask(a)
    H,W=y.shape
    # smooth supersampled shape
    big=np.asarray(Image.fromarray((y*255).astype('uint8')).resize((W*SS,H*SS),Image.BICUBIC)).astype(float)/255
    big=ndi.gaussian_filter(big,SS*0.7)>0.5
    d_in=ndi.distance_transform_edt(big)/SS
    d_out=ndi.distance_transform_edt(~big)/SS
    def cov(dist_signed,w=1.0/SS): return np.clip(0.5-dist_signed/ (w*1.0)/1.0,0,1)
    sd=(d_out-d_in)  # signed distance, negative inside
    # layers at SS res
    out_cov=np.clip((OUT_W-sd)/ (1.0/SS) *0.5+0.5,0,1)*0+ (sd<=OUT_W)
    out_shape=(sd<=OUT_W)
    sh_shape=np.roll(np.roll(out_shape,SH_DY*SS,0),SH_DX*SS,1)
    rim=big&(d_in<=RIM_W)
    canvas=np.zeros((H*SS,W*SS,4),float)
    def paint(m,col):
        canvas[m,:3]=col; canvas[m,3]=255
    paint(sh_shape,SH); paint(out_shape,OUT); paint(big,FILL); paint(rim,RIM)
    # premultiplied downsample
    pm=canvas.copy(); pm[...,:3]*=pm[...,3:4]/255
    small=pm.reshape(H,SS,W,SS,4).mean((1,3))
    al=small[...,3]; rgb=np.where(al[...,None]>0,small[...,:3]/np.maximum(al[...,None],1e-6)*255,0)
    outa=np.dstack([np.clip(rgb,0,255),al]).astype('uint8')
    return Image.fromarray(outa,'RGBA')
if __name__=='__main__':
    import sys
    for src,dst in zip(sys.argv[1::2],sys.argv[2::2]):
        normalize(Image.open(src)).save(dst)
