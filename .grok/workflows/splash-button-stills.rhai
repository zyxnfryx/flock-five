let meta = #{
    name: "splash-button-stills",
    description: "Pull playtest/tonight, capture splash LEVEL 1/2/4/6/999 stills, commit and push",
    when_to_use: "When splash play-flower button type changes, or to recapture blank LEVEL 1, Easy / Super Easy / Super Duper Easy, and LEVEL 999 stills for the bots",
    phases: [
        #{ title: "Capture", detail: "pull tip, rebuild, shoot LEVEL 1/2/4/6/999, push stills" },
        #{ title: "Verify", detail: "confirm the five stills are on origin" },
    ],
};

let force = false;
if args != () && args.force == true { force = true; }

phase("Capture");
let cap = "";
cap += "Flock Five splash stills. Use run_terminal_command. Do not answer from memory.\n";
cap += "Project: /Users/zfxgames/wkspaces/flock-five\n";
cap += "Branch: playtest/tonight\n";
cap += "Run this script and use its last stdout line:\n";
cap += "  bash /Users/zfxgames/.grok/long-running-background-tasks/flock_five_poll_splash_buttons.sh\n";
if force {
    cap += "Set FORCE=1 in the environment so it recaptures even if HEAD already matches origin.\n";
    cap += "Example: FORCE=1 bash /Users/zfxgames/.grok/long-running-background-tasks/flock_five_poll_splash_buttons.sh\n";
}
cap += "Last-line meanings: NO_NEW_TIP = stop, do not recapture. STILLS_UNCHANGED = pulled but PNGs identical, do not empty-commit. PUSHED <sha> = done. FAILED = debug, fix, retry the script once.\n";
cap += "Do not merge to main. Do not commit splash-intro.wav, Assets/_Recovery/, or fx_ad_sign.png.meta pivot flips (keep spritePivot 0.5, 0.92).\n";
cap += "If the script is missing, do the pipeline yourself: git fetch and git pull --ff-only origin playtest/tonight; rebuild Unity 6000.3.22f1 so Assembly-CSharp.dll is newer than FlockFiveApp.cs (quit the editor if stale, not AssetImportWorker); write /tmp/flock-five-level6 and /tmp/flock-five-play; wait for /tmp/paradice/splash-joke-blank.png, splash-joke-easy.png, splash-joke-super-easy.png, splash-joke-super-duper-easy.png, splash-level-999.png; copy them to Playtest/splash-buttons/; git add those five PNGs; commit; git push origin playtest/tonight.\n";
cap += "ShotSplashButtons forces LEVEL 1 blank (no joke), LEVEL 2 Easy, LEVEL 4 Super Easy, LEVEL 6 Super Duper Easy, and LEVEL 999 with Super Duper Easy.\n";
cap += "Return one line starting with NO_NEW_TIP, STILLS_UNCHANGED, PUSHED, or FAILED.\n";

let cap_r = agent(cap, #{
    label: "capture-stills",
    capability_mode: "all",
});

let cap_ok = cap_r != () && cap_r.success == true;
if !cap_ok {
    log("capture agent failed");
    complete(#{ ok: false, summary: "Capture agent failed" });
}

phase("Verify");
let ver = "";
ver += "Confirm Flock Five splash stills on origin. Use run_terminal_command. Do not answer from memory.\n";
ver += "cd /Users/zfxgames/wkspaces/flock-five && git fetch origin && git log -1 --oneline origin/playtest/tonight && git ls-tree -l origin/playtest/tonight Playtest/splash-buttons/\n";
ver += "Confirm these five blobs exist: splash-joke-blank.png, splash-joke-easy.png, splash-joke-super-easy.png, splash-joke-super-duper-easy.png, splash-level-999.png.\n";
ver += "Return one line: OK <sha> or MISSING.\n";

let ver_r = agent(ver, #{
    label: "verify-origin",
    capability_mode: "read-only",
});

let ver_ok = ver_r != () && ver_r.success == true;
if !ver_ok {
    log("verify agent failed");
    complete(#{ ok: false, summary: "Verify agent failed after capture" });
}

log("splash-button-stills finished");
complete(#{ ok: true, summary: "Splash LEVEL 1/2/4/6/999 stills pipeline finished" });
