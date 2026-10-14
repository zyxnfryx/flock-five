# name, before path, play gain in game, force_mono(before meta), normalize(before meta), class, target LUFS
B='/workspace/ff/smooth/before/'
T=[
 ('splash-theme',  B+'assets/splash-theme.wav',   0.52,1,1,'bed',-17.0,'Audio/Bed/splash-theme.wav'),
 ('garden-theme',  B+'assets/garden-theme.wav',   0.24,1,1,'bed',-17.0,'Audio/Bed/garden-theme.wav'),
 ('badger-battle', B+'assets/badger-battle.wav',  0.40,1,1,'bed',-17.0,'Audio/Bed/badger-battle.wav'),
 ('badger-entrance',B+'assets/badger-entrance.wav',0.40,1,1,'sting',-16.0,'Audio/Bed/badger-entrance.wav'),
 ('badger-win',    B+'assets/badger-win.wav',     0.40,1,1,'sting',-16.0,'Audio/Bed/badger-win.wav'),
 ('badger-lose',   B+'assets/badger-lose.wav',    0.40,1,1,'sting',-16.0,'Audio/Bed/badger-lose.wav'),
 ('zfx-jingle v4.1 (shipped ogg)',B+'assets/zfx_jingle.ogg',0.90,0,0,'sting',-16.0,'Audio/zfx_jingle.ogg'),
 ('proc dawn (fallback)',B+'proc/proc-dawn.wav',  0.24,0,0,'bed',-17.0,None),
 ('proc mid (fallback)', B+'proc/proc-mid.wav',   0.24,0,0,'bed',-17.0,None),
 ('proc last-light (fallback)',B+'proc/proc-lastlight.wav',0.24,0,0,'bed',-17.0,None),
 ('proc splash (fallback)',B+'proc/proc-splash.wav',0.52,0,0,'bed',-17.0,None),
 ('proc storm rain', B+'proc/proc-storm-rain.wav',0.17,0,0,'bed',-17.0,None),
 ('proc combo fifth',B+'proc/proc-combo-fifth.wav',0.04,0,0,'bed',-17.0,None),
]+[(f'proc combo jingle x{s}',B+f'proc/proc-combo-jingle-{s}.wav',0.76,0,0,'sting',-16.0,None) for s in range(2,9)]+[
 ('proc celebrate',  B+'proc/proc-celebrate.wav', 0.80,0,0,'sting',-16.0,None)]
