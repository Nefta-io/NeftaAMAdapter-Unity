#import <Foundation/Foundation.h>
#import "GADNeftaExtras.h"
#import <NeftaSDK/NeftaSDK-Swift.h>

@interface NeftaInterstitial : NSObject <GADMediationInterstitialAd, NInterstitialListener>
@property NInterstitial * _Nonnull interstitial;
@property GADMediationInterstitialLoadCompletionHandler _Nullable listener;
@property (nonatomic, weak) NSString *errorDomain;
@property (nonatomic, weak) GADNeftaExtras *extras;
@property (nonatomic, weak) id<GADMediationInterstitialAdEventDelegate> adEventDelegate;
+ (instancetype _Nonnull)Init:(NSString *_Nonnull)id listener:(nonnull GADMediationInterstitialLoadCompletionHandler)listener  errorDomain:(NSString *_Nonnull)errorDomain;
- (void) Load;
@end
