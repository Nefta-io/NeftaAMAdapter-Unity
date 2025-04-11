#import <Foundation/Foundation.h>
#import "GADNeftaExtras.h"
#import <NeftaSDK/NeftaSDK-Swift.h>

@interface NeftaRewarded : NSObject <GADMediationRewardedAd, NRewardedListener>
@property NRewarded * _Nonnull rewarded;
@property GADMediationRewardedLoadCompletionHandler _Nullable listener;
@property (nonatomic, weak) NSString *errorDomain;
@property (nonatomic, weak) GADNeftaExtras *extras;
@property (nonatomic, weak) id<GADMediationRewardedAdEventDelegate> adEventDelegate;
+ (instancetype _Nonnull)Init:(NSString *_Nonnull)id listener:(nonnull GADMediationRewardedLoadCompletionHandler)listener errorDomain:(NSString *_Nonnull)errorDomain;
- (void) Load;
@end
