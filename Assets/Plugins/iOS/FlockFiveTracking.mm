#import <Foundation/Foundation.h>
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

extern "C" void FlockFive_RequestTracking(void)
{
    if (@available(iOS 14, *))
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                FlockFive_SendAtt((int)status);
            }];
        });
    }
    else
    {
        FlockFive_SendAtt(3);
    }
}
