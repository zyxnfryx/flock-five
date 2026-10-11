#import <UIKit/UIKit.h>
#import <AVFoundation/AVFoundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

extern void UnitySendMessage(const char *obj, const char *method, const char *msg);

// 0 = not determined, 1 = restricted, 2 = denied, 3 = authorized.
extern "C" int FlockFive_TrackingStatus(void)
{
    if (@available(iOS 14, *))
        return (int)[ATTrackingManager trackingAuthorizationStatus];
    return 3;
}

static void FlockFive_SendAtt(int status)
{
    // Hop to the main queue. The ATT completion is not guaranteed to be there,
    // and UnitySendMessage must run on the Unity player thread.
    dispatch_async(dispatch_get_main_queue(), ^{
        NSString *payload = [NSString stringWithFormat:@"%d", status];
        UnitySendMessage("Ads", "OnAttComplete", payload.UTF8String);
    });
}

// One in-flight prompt. A second call while the system dialog is up is ignored.
static BOOL s_attInFlight;
static id s_attObserver;

static void FlockFive_WaitUntilActive(void);
static void FlockFive_RequestTrackingNow(void)
{
    if (s_attInFlight) return;
    s_attInFlight = YES;
    if (@available(iOS 14, *))
    {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            s_attInFlight = NO;
            FlockFive_SendAtt((int)status);
        }];
    }
    else
    {
        s_attInFlight = NO;
        FlockFive_SendAtt(3);
    }
}

// Apple drops the prompt, and may never call the handler, unless the
// application is active. If it is not, wait for DidBecomeActive.
static void FlockFive_WaitUntilActive(void)
{
    if (s_attObserver != nil) return;
    s_attObserver = [[NSNotificationCenter defaultCenter]
        addObserverForName:UIApplicationDidBecomeActiveNotification
                    object:nil
                     queue:[NSOperationQueue mainQueue]
                usingBlock:^(NSNotification *note) {
        (void)note;
        id obs = s_attObserver;
        s_attObserver = nil;
        if (obs != nil)
            [[NSNotificationCenter defaultCenter] removeObserver:obs];
        UIApplication *app = [UIApplication sharedApplication];
        if (app != nil && app.applicationState != UIApplicationStateActive)
        {
            FlockFive_WaitUntilActive();
            return;
        }
        FlockFive_RequestTrackingNow();
    }];
}

extern "C" void FlockFive_RequestTracking(void)
{
    if (@available(iOS 14, *))
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            UIApplication *app = [UIApplication sharedApplication];
            if (app != nil && app.applicationState == UIApplicationStateActive)
                FlockFive_RequestTrackingNow();
            else
                FlockFive_WaitUntilActive();
        });
    }
    else
    {
        FlockFive_SendAtt(3);
    }
}

// simctl launch args are on the process. IL2CPP's GetCommandLineArgs is not:
// UnityInitScripting calls InitializeIl2CppFromMain with argc 1.
extern "C" int FlockFive_HasLaunchArg(const char *needle)
{
    if (needle == NULL || needle[0] == '\0') return 0;
    @autoreleasepool
    {
        NSArray<NSString *> *args = [[NSProcessInfo processInfo] arguments];
        if (args == nil) return 0;
        NSString *want = [NSString stringWithUTF8String:needle];
        if (want == nil) return 0;
        for (NSString *arg in args)
        {
            if (arg != nil && [arg isEqualToString:want]) return 1;
        }
    }
    return 0;
}

// Game session. muteOtherAudioSources is off, so Ambient (silent switch
// honored, other apps keep playing). SoloAmbient is the exclusive twin and
// also honors the switch. Options stay 0: no DuckOthers, no MixWithOthers.
// An interruption end is forwarded to AudioGuard.Restore. ShouldResume is
// not required; iOS often omits it and the mix stays ducked if we wait for it.
static id s_audioObs;
static BOOL s_audioInstalled;

extern "C" void FlockFive_AudioInstall(void)
{
    if (s_audioInstalled) return;
    s_audioInstalled = YES;
    AVAudioSession *session = [AVAudioSession sharedInstance];
    s_audioObs = [[NSNotificationCenter defaultCenter]
        addObserverForName:AVAudioSessionInterruptionNotification
                    object:session
                     queue:[NSOperationQueue mainQueue]
                usingBlock:^(NSNotification *note) {
        NSNumber *typeNum = note.userInfo[AVAudioSessionInterruptionTypeKey];
        if (typeNum == nil) return;
        if (typeNum.unsignedIntegerValue != AVAudioSessionInterruptionTypeEnded) return;
        UnitySendMessage("AudioGuard", "OnAudioInterruption", "ended");
    }];
}

extern "C" void FlockFive_AudioActivate(int solo)
{
    AVAudioSession *session = [AVAudioSession sharedInstance];
    AVAudioSessionCategory category = solo
        ? AVAudioSessionCategorySoloAmbient
        : AVAudioSessionCategoryAmbient;
    [session setCategory:category withOptions:0 error:nil];
    [session setActive:YES error:nil];
}
