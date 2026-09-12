let meta = #{
    name: "splash-button-stills",
    description: "Pull playtest/tonight, capture LEVEL 1 splash still and finale smash clip, commit and push",
    when_to_use: "When the home logo or round-end logo changes, or to recapture LEVEL 1 and the smash-through clip for the bots",
    phases: [
        #{ title: "Capture", detail: "pull tip, rebuild, shoot LEVEL 1 and finale smash, push" },
        #{ title: "Verify", detail: "confirm the LEVEL 1 still and smash clip are on origin" },
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
cap += "Last-line meanings: NO_NEW_TIP = stop, do not recapture. STILLS_UNCHANGED = pulled but files identical, do not empty-commit. PUSHED <sha> = done. FAILED = debug, fix, retry the script once.\n";
cap += "Do not merge to main. Do not commit splash-intro.wav, Assets/_Recovery/, or fx_ad_sign.png.meta pivot flips (keep spritePivot 0.5, 0.92).\n";
cap += "If the script is missing, do the pipeline yourself: git fetch and git pull --ff-only origin playtest/tonight; rebuild Unity 6000.3.22f1 so Assembly-CSharp.dll is newer than FlockFiveApp.cs (quit the editor if stale, not AssetImportWorker); write /tmp/flock-five-level6 and /tmp/flock-five-play; wait for /tmp/paradice/splash-joke-blank.png and /tmp/paradice/finale-smash.mp4; copy them to Playtest/splash-buttons/; git add those two files; commit; git push origin playtest/tonight.\n";
cap += "ShotSplashButtons now captures LEVEL 1 blank (home logo) and a short video of end-of-play through the birds smash-through. Do not capture LEVEL 2/4/6/1000.\n";
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
ver += "Confirm splash-joke-blank.png and finale-smash.mp4 exist. LEVEL 2/4/6/1000 stills should be gone.\n";
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
complete(#{ ok: true, summary: "LEVEL 1 still and finale smash clip pipeline finished" });
