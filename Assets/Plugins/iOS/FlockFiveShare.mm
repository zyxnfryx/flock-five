#import <UIKit/UIKit.h>

extern void UnitySendMessage(const char *obj, const char *method, const char *msg);
extern UIViewController *UnityGetGLViewController(void);

extern "C" void FlockFive_Share(const char *text)
{
    if (text == NULL) return;
    NSString *body = [NSString stringWithUTF8String:text];
    UIActivityViewController *sheet =
        [[UIActivityViewController alloc] initWithActivityItems:@[body] applicationActivities:nil];
    sheet.completionWithItemsHandler = ^(UIActivityType type, BOOL completed, NSArray *items, NSError *err)
    {
        UnitySendMessage("FlockFiveApp", "InviteShareDone", completed ? "1" : "0");
    };
    UIViewController *root = UnityGetGLViewController();
    UIPopoverPresentationController *pop = sheet.popoverPresentationController;
    if (pop != nil)
    {
        pop.sourceView = root.view;
        pop.sourceRect = CGRectMake(CGRectGetMidX(root.view.bounds), CGRectGetMidY(root.view.bounds), 1, 1);
        pop.permittedArrowDirections = 0;
    }
    [root presentViewController:sheet animated:YES completion:nil];
}
