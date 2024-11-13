#import <Foundation/Foundation.h>
#import "NeftaExtras.h"
#import <NeftaSDK/NeftaSDK-Swift.h>

@interface NeftaRewarded : NSObject <GADMediationRewardedAd, NRewardedListener>
@property NRewarded * _Nonnull rewarded;
@property GADMediationRewardedLoadCompletionHandler _Nullable listener;
@property (nonatomic, weak) NSString *errorDomain;
@property (nonatomic, weak) NeftaExtras *extras;
@property (nonatomic, weak) id<GADMediationRewardedAdEventDelegate> adEventDelegate;
+ (instancetype _Nonnull)Init:(NSString *_Nonnull)id listener:(nonnull GADMediationRewardedLoadCompletionHandler)listener errorDomain:(NSString *_Nonnull)errorDomain;
- (void) Load;
@end
