#import <Foundation/Foundation.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

#import "GADNeftaExtras.h"
#import <NeftaSDK/NeftaSDK-Swift.h>

@interface NeftaAdapter : NSObject <GADMediationAdapter>
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
+ (void)OnExternalMediationRequestLoad:(AdType)adType recommendedAdUnitId:(NSString * _Nullable)recommendedAdUnitId calculatedFloorPrice:(double)calculatedFloorPrice adUnitId:(NSString * _Nonnull)adUnitId;
+ (void)OnExternalMediationRequestFail:(AdType)adType recommendedAdUnitId:(NSString * _Nullable)recommendedAdUnitId calculatedFloorPrice:(double)calculatedFloorPrice  adUnitId:(NSString * _Nonnull)adUnitId error:(NSError * _Nullable)error;
+ (void)OnExternalMediationRequestLoad:(AdType)adType requestedFloorPrice:(double)requestedFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice adUnitId:(NSString * _Nonnull)adUnitId;
+ (void)OnExternalMediationRequestFail:(AdType)adType requestedFloorPrice:(double)requestedFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice adUnitId:(NSString * _Nonnull)adUnitId error:(NSError * _Nullable)error;
+ (void)OnExternalMediationImpression:(GADAdValue* _Nonnull)adValue;

+ (NSError *_Nonnull) NLoadToAdapterError:(NError *_Nonnull)error;
@end
