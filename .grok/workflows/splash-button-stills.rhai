let meta = #{
    name: "splash-button-stills",
    description: "Pull playtest/tonight, capture a quick splash-screen clip of birds around the logo, commit and push",
    when_to_use: "When splash logo or title birds change, or to recapture a short home-screen flutter clip for the bots",
    phases: [
        #{ title: "Capture", detail: "pull tip, rebuild, shoot splash birds clip, push" },
        #{ title: "Verify", detail: "confirm splash-birds.mp4 is on origin" },
    ],
};

let force = false;
if args != () && args.force == true { force = true; }

phase("Capture");
let cap = "";
cap += "Flock Five splash clip. Use run_terminal_command. Do not answer from memory.\n";
cap += "Project: /Users/zfxgames/wkspaces/flock-five\n";
cap += "Branch: playtest/tonight\n";
cap += "Run this script and use its last stdout line:\n";
cap += "  bash /Users/zfxgames/.grok/long-running-background-tasks/flock_five_poll_splash_buttons.sh\n";
if force {
    cap += "Set FORCE=1 in the environment so it recaptures even if HEAD already matches origin.\n";
    cap += "Example: FORCE=1 bash /Users/zfxgames/.grok/long-running-background-tasks/flock_five_poll_splash_buttons.sh\n";
}
cap += "Last-line meanings: NO_NEW_TIP = stop. SKIP_BUSY = another capture is running, stop. STILLS_UNCHANGED = identical files, do not empty-commit. PUSHED <sha> = done. FAILED = debug, retry once.\n";
cap += "Do not merge to main. Do not commit splash-intro.wav, Assets/_Recovery/, or fx_ad_sign.png.meta pivot flips (keep spritePivot 0.5, 0.92).\n";
cap += "ShotSplashButtons now records only a short splash-screen video (birds around the home title). Do not capture LEVEL 2/4/6/1000 stills or the finale smash clip.\n";
cap += "Return one line starting with NO_NEW_TIP, SKIP_BUSY, STILLS_UNCHANGED, PUSHED, or FAILED.\n";

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
ver += "Confirm Flock Five splash clip on origin. Use run_terminal_command. Do not answer from memory.\n";
ver += "cd /Users/zfxgames/wkspaces/flock-five && git fetch origin && git log -1 --oneline origin/playtest/tonight && git ls-tree -l origin/playtest/tonight Playtest/splash-buttons/\n";
ver += "Confirm splash-birds.mp4 exists.\n";
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
complete(#{ ok: true, summary: "Splash birds clip pipeline finished" });
