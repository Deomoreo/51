#import <UIKit/UIKit.h>
#include <atomic>

static std::atomic<int> generation(0);
extern "C" void K6SetGeneration(int value) { generation.store(value); }

extern "C" void K6Impact(int strong, int requestedGeneration)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (generation.load() != requestedGeneration) return;
        if (@available(iOS 10.0, *))
        {
            static UIImpactFeedbackGenerator *light;
            static UIImpactFeedbackGenerator *medium;
            if (!light) light = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            if (!medium) medium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            UIImpactFeedbackGenerator *generator = strong ? medium : light;
            [generator prepare];
            [generator impactOccurred];
        }
    });
}
