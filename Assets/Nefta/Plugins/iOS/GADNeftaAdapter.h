#import <Foundation/Foundation.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

#import "GADNeftaExtras.h"
#import <NeftaSDK/NeftaSDK-Swift.h>

@interface GADNeftaAdapter : NSObject <GADMediationAdapter>
typedef NS_ENUM(NSInteger, NeftaAdapterErrorCode) {
    NeftaAdapterErrorCodeInvalidServerParameters = 101,
    NeftaAdapterErrorCodeAdNotReady = 102,
};
typedef NS_ENUM(NSInteger, AdType) {
    AdTypeOther = 0,
    AdTypeBanner = 1,
    AdTypeInterstitial = 2,
    AdTypeRewarded = 3
};

+ (void) OnExternalMediationRequestWithInsight:(AdInsight * _Nonnull)insight request:(GADRequest * _Nonnull)request adUnitId:(NSString * _Nonnull)adUnitId;
+ (void) OnExternalMediationRequest:(AdType)adType request:(GADRequest * _Nonnull)request adUnitId:(NSString * _Nonnull)adUnitId;

+ (void) OnExternalMediationRequestLoadWithBanner:(GADBannerView * _Nonnull)banner request:(GADRequest * _Nonnull)request;
+ (void) OnExternalMediationRequestLoadWithInterstitial:(GADInterstitialAd * _Nonnull)interstitial request:(GADRequest * _Nonnull)request;
+ (void) OnExternalMediationRequestLoadWithRewarded:(GADRewardedAd * _Nonnull)rewarded request:(GADRequest * _Nonnull)request;
+ (void) OnExternalMediationRequestFail:(GADRequest * _Nonnull)request error:(NSError * _Nonnull)error;

+ (void)OnExternalMediationImpressionWithBanner:(GADBannerView * _Nonnull)banner adValue:(GADAdValue* _Nonnull)adValue;
+ (void)OnExternalMediationImpressionWithInterstitial:(GADInterstitialAd * _Nonnull)interstitial adValue:(GADAdValue* _Nonnull)adValue;
+ (void)OnExternalMediationImpressionWithRewarded:(GADRewardedAd * _Nonnull)rewarded adValue:(GADAdValue* _Nonnull)adValue;

+ (void)OnExternalMediationClickWithBanner:(GADBannerView * _Nonnull)banner;
+ (void)OnExternalMediationClickWithInterstitial:(GADInterstitialAd * _Nonnull)interstitial;
+ (void)OnExternalMediationClickWithRewarded:(GADRewardedAd * _Nonnull)rewarded;

+ (NSError *_Nonnull) NLoadToAdapterError:(NError *_Nonnull)error;
@end
