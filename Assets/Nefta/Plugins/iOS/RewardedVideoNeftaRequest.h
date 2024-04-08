#import "NeftaRequest.h"

@interface RewardedVideoNeftaRequest : NSObject <GADMediationRewardedAd, NeftaRequest>

@property GADMediationRewardedLoadCompletionHandler _Nullable callback;
@property BOOL muteAudio;

+ (instancetype _Nonnull)Init:(NeftaAdapter *_Nonnull)adapter placementId:(NSString *_Nonnull)placementId callback:(nonnull GADMediationRewardedLoadCompletionHandler)callback;

@end
