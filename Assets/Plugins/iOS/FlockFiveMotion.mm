#import <UIKit/UIKit.h>

// 1 when Settings > Accessibility > Motion > Reduce Motion is on.
extern "C" int FlockFive_ReduceMotion(void)
{
    return UIAccessibilityIsReduceMotionEnabled() ? 1 : 0;
}
