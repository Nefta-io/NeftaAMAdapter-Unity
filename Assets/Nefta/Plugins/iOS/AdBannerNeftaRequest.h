#import "NeftaRequest.h"

@interface AdBannerNeftaRequest : NSObject<GADMediationBannerAd, NeftaRequest>

@property GADMediationBannerLoadCompletionHandler _Nullable callback;

+ (instancetype _Nonnull)Init:(NeftaAdapter *_Nonnull)adapter  placementId:(NSString *_Nonnull)placementId callback:(nonnull GADMediationBannerLoadCompletionHandler)callback;

@end
