import json,sys; sys.path.insert(0,'.')
from smooth import *; from inventory import T
res={}
for name,p,g,fm,nm,cl,tg,dst in T:
    x=as_imported(load(p),fm,nm); res[name]=metrics(x,g)
    r=res[name]; print(f"{name:28s} LUFS {r['lufs']:6.1f} TP {r['tp']:5.1f} crest {r['crest']:5.1f} e38 {r['e38']:5.2f}% sib {r['sib']:6.1f} res {r['res_db']:4.1f}@{r['res_hz']:.0f} | ph harsh {r['ph_harsh']:5.1f} sib {r['ph_sib']:6.1f} clip {r['ph_clip']:5.2f}% dist {r['ph_dist']:6.1f}",flush=True)
json.dump(res,open('../before.json','w'),indent=1)
