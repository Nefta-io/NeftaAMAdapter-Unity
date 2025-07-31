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
+ (void)OnExternalMediationRequestLoadWithBanner:(GADBannerView * _Nonnull)banner usedInsight:(AdInsight * _Nullable)usedInsight;
+ (void)OnExternalMediationRequestLoadWithInterstitial:(GADInterstitialAd * _Nonnull)interstitial usedInsight:(AdInsight * _Nullable)usedInsight;
+ (void)OnExternalMediationRequestLoadWithRewarded:(GADRewardedAd * _Nonnull)rewarded usedInsight:(AdInsight * _Nullable)usedInsight;
+ (void)OnExternalMediationRequestFail:(AdType)adType adUnitId:(NSString * _Nonnull)adUnitId usedInsight:(AdInsight * _Nullable)usedInsight error:(NSError * _Nullable)error;
+ (void)OnExternalMediationImpressionWithBanner:(GADBannerView * _Nonnull)banner adValue:(GADAdValue* _Nonnull)adValue;
+ (void)OnExternalMediationImpressionWithInterstitial:(GADInterstitialAd * _Nonnull)interstitial adValue:(GADAdValue* _Nonnull)adValue;
+ (void)OnExternalMediationImpressionWithRewarded:(GADRewardedAd * _Nonnull)rewarded adValue:(GADAdValue* _Nonnull)adValue;

+ (void)OnExternalMediationImpressionAsString:(int)adType network:(NSString * _Nonnull)network data:(NSString * _Nonnull)data revenue:(double)revenue precision:(NSString * _Nonnull) precision;

+ (NSError *_Nonnull) NLoadToAdapterError:(NError *_Nonnull)error;
@end
