#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

// 0 = not determined, 1 = restricted, 2 = denied, 3 = authorized.
extern "C" int FlockFive_TrackingStatus(void)
{
    if (@available(iOS 14, *))
        return (int)[ATTrackingManager trackingAuthorizationStatus];
    return 3;
}

extern "C" void FlockFive_RequestTracking(void)
{
    if (@available(iOS 14, *))
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {}];
        });
    }
}
