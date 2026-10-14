import json,sys; sys.path.insert(0,'.')
from smooth import *; from inventory import T
before=json.load(open('../before.json'))
NEW={'proc dawn (fallback)':0.30,'proc mid (fallback)':0.30,'proc last-light (fallback)':0.30,'proc splash (fallback)':0.76,'proc storm rain':0.17,'proc combo fifth':0.04,'proc celebrate':0.50}
NEW.update({f'proc combo jingle x{s}':0.48 for s in range(2,9)})
out={}
for name,p,g,fm,nm,cl,tg,dst in T:
    if not name.startswith('proc'): continue
    y=as_imported(load(p.replace('/before/','/after/')),0,0); ng=NEW[name]
    r=metrics(y,ng); r['game']=lufs(y)+20*np.log10(ng); r['bgame']=before[name]['lufs']+20*np.log10(g); r['gain']=ng
    if name in ('proc dawn (fallback)','proc mid (fallback)','proc last-light (fallback)','proc splash (fallback)','proc combo fifth','proc storm rain'):
        r['seam']=float(np.abs(y[0]-y[-1]).max()); r['seam_before']=float(np.abs(load(p)[0]-load(p)[-1]).max())
    out[name]=r; b=before[name]
    print(f"{name:28s} gain {g:.2f}->{ng:.2f} LUFS {b['lufs']:6.1f}->{r['lufs']:6.1f} TP {b['tp']:5.1f}->{r['tp']:5.1f} e38 {b['e38']:5.2f}->{r['e38']:5.2f} | ph harsh {b['ph_harsh']:6.1f}->{r['ph_harsh']:6.1f} clip {b['ph_clip']:5.2f}->{r['ph_clip']:5.2f} | game {r['bgame']:.1f}->{r['game']:.1f} seam {r.get('seam_before')}->{r.get('seam')}")
# BeeFound: jingle x2 at 0.52 unchanged
y=as_imported(load('../after/proc/proc-combo-jingle-2.wav'),0,0); print('BeeFound x2 @0.52 phone clip',phone(y,0.52)[1])
json.dump(out,open('../after_proc.json','w'),indent=1)
