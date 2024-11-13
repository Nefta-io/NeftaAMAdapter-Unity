#import <Foundation/Foundation.h>
#import <GoogleMobileAds/GoogleMobileAds.h>
#import <NeftaSDK/NeftaSDK-Swift.h>

@interface NeftaBanner : NSObject<GADMediationBannerAd, NBannerListener>
@property NBanner * _Nonnull banner;
@property GADMediationBannerLoadCompletionHandler _Nullable listener;
@property (nonatomic, weak) NSString *errorDomain;
@property (nonatomic, weak) id<GADMediationBannerAdEventDelegate> adEventDelegate;
+ (instancetype _Nonnull)Init:(NSString *_Nonnull)id listener:(nonnull GADMediationBannerLoadCompletionHandler)listener errorDomain:(NSString * _Nonnull)errorDomain;
- (void)Load;
@end
