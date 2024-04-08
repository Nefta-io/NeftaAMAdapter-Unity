#import "NeftaRequest.h"

@interface InterstitialNeftaRequest : NSObject <GADMediationInterstitialAd, NeftaRequest>

@property GADMediationInterstitialLoadCompletionHandler _Nullable callback;

+ (instancetype _Nonnull)Init:(NeftaAdapter *_Nonnull)adapter  placementId:(NSString *_Nonnull)placementI callback:(nonnull GADMediationInterstitialLoadCompletionHandler)callback;

@end
