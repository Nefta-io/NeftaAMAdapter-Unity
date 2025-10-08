#import "GADNeftaAdapter.h"
#import "GADNeftaBanner.h"
#import "GADNeftaInterstitial.h"
#import "GADNeftaRewarded.h"

NSString * const _mediationProvider = @"google-admob";

@implementation GADNeftaAdapter

+ (Class<GADAdNetworkExtras>)networkExtrasClass {
    return [GADNeftaExtras class];
}

+ (void) OnExternalMediationRequestWithInsight:(AdInsight * _Nonnull)insight request:(GADRequest * _Nonnull)request adUnitId:(NSString *)adUnitId {
    NSString *id0 = [NSString stringWithFormat:@"%lu", (unsigned long)[request hash]];
    int adType = (int) insight._type;
    int adOpportunityId = (int)insight._adOpportunityId;
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: adType id: id0 requestedAdUnitId: adUnitId requestedFloorPrice: -1 adOpportunityId: adOpportunityId];
}

+ (void) OnExternalMediationRequest:(AdType)adType request:(GADRequest * _Nonnull)request adUnitId:(NSString *)adUnitId {
    NSString *id0 = [NSString stringWithFormat:@"%lu", (unsigned long)[request hash]];
    [NeftaPlugin OnExternalMediationRequest: _mediationProvider adType: (int)adType id: id0 requestedAdUnitId: adUnitId requestedFloorPrice: -1 adOpportunityId: -1];
}

+ (void) OnExternalMediationRequestLoadWithBanner:(GADBannerView * _Nonnull)banner request:(GADRequest * _Nonnull)request {
    NSString *id0 = [NSString stringWithFormat:@"%lu", (unsigned long)[request hash]];
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[banner hash]];
    [GADNeftaAdapter OnExternalMediationResponse: id0 id2: id2];
}
+ (void) OnExternalMediationRequestLoadWithInterstitial:(GADInterstitialAd * _Nonnull)interstitial request:(GADRequest * _Nonnull)request {
    NSString *id0 = [NSString stringWithFormat:@"%lu", (unsigned long)[request hash]];
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[interstitial hash]];
    [GADNeftaAdapter OnExternalMediationResponse: id0 id2: id2];
}
+ (void) OnExternalMediationRequestLoadWithRewarded:(GADRewardedAd * _Nonnull)rewarded request:(GADRequest * _Nonnull)request {
    NSString *id0 = [NSString stringWithFormat:@"%lu", (unsigned long)[request hash]];
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[rewarded hash]];
    [GADNeftaAdapter OnExternalMediationResponse: id0 id2: id2];
}
+ (void) OnExternalMediationResponse:(NSString *)id0 id2:(NSString * _Nonnull)id2 {
    [NeftaPlugin OnExternalMediationResponse: _mediationProvider id: id0 id2: id2 revenue: -1 precision: nil status: 1 providerStatus: nil networkStatus: nil];
}

+ (void) OnExternalMediationRequestFail:(GADRequest * _Nonnull)request error:(NSError *)error {
    int status = 0;
    if (error != nil && error.code == GADErrorNoFill) {
        status = 2;
    }
    NSString *providerStatus = [NSString stringWithFormat:@"%ld", error.code];
    NSString *networkStatus = nil;
    GADResponseInfo *responseInfo = error.userInfo[GADErrorUserInfoKeyResponseInfo];
    if (responseInfo != nil) {
        GADAdNetworkResponseInfo *adapterResponse = responseInfo.loadedAdNetworkResponseInfo;
        if (adapterResponse != nil) {
            NSError *adapterError = adapterResponse.error;
            if (adapterError != nil) {
                networkStatus = [NSString stringWithFormat:@"%ld", adapterError.code];
            }
        }
    }
    NSString *id0 = [NSString stringWithFormat:@"%lu", (unsigned long)[request hash]];
    
    [NeftaPlugin OnExternalMediationResponse: _mediationProvider id: id0 id2: nil revenue: -1 precision: nil status: status providerStatus: providerStatus networkStatus: networkStatus];
}

+ (void) OnExternalMediationImpressionWithBanner:(GADBannerView * _Nonnull)banner adValue:(GADAdValue*)adValue {
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[banner hash]];
    [GADNeftaAdapter OnExternalMediationImpression: false type: 1 adUnitId: banner.adUnitID id2: id2 responseInfo: banner.responseInfo adValue: adValue];
}
+ (void) OnExternalMediationImpressionWithInterstitial:(GADInterstitialAd * _Nonnull)interstitial adValue:(GADAdValue*)adValue {
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[interstitial hash]];
    [GADNeftaAdapter OnExternalMediationImpression: false type: 2 adUnitId: interstitial.adUnitID id2: id2 responseInfo: interstitial.responseInfo adValue: adValue];
}
+ (void) OnExternalMediationImpressionWithRewarded:(GADRewardedAd * _Nonnull)rewarded adValue:(GADAdValue*)adValue {
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[rewarded hash]];
    [GADNeftaAdapter OnExternalMediationImpression: false type: 3 adUnitId: rewarded.adUnitID id2: id2 responseInfo: rewarded.responseInfo adValue: adValue];
}

+ (void) OnExternalMediationClickWithBanner:(GADBannerView * _Nonnull)banner {
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[banner hash]];
    [GADNeftaAdapter OnExternalMediationImpression: true type: 1 adUnitId: banner.adUnitID id2: id2 responseInfo: banner.responseInfo adValue: nil];
}
+ (void) OnExternalMediationClickWithInterstitial:(GADInterstitialAd * _Nonnull)interstitial {
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[interstitial hash]];
    [GADNeftaAdapter OnExternalMediationImpression: true type: 2 adUnitId: interstitial.adUnitID id2: id2 responseInfo: interstitial.responseInfo adValue: nil];
}
+ (void) OnExternalMediationClickWithRewarded:(GADRewardedAd * _Nonnull)rewarded {
    NSString *id2 = [NSString stringWithFormat:@"%lu", (unsigned long)[rewarded hash]];
    [GADNeftaAdapter OnExternalMediationImpression: true type: 3 adUnitId: rewarded.adUnitID id2: id2 responseInfo: rewarded.responseInfo adValue: nil];
}

+ (void) OnExternalMediationImpression:(BOOL)isClick type:(int)type adUnitId:(NSString *)adUnitId id2:(NSString * _Nonnull)id2 responseInfo:(GADResponseInfo * _Nullable)responseInfo adValue:(GADAdValue*)adValue {
    NSMutableDictionary *data = [NSMutableDictionary dictionary];
    NSString *adType = nil;
    if (type == 1) {
        adType = @"banner";
    } else if (type == 2) {
        adType = @"interstitial";
    } else if (type == 3) {
        adType = @"rewarded";
    } else {
        adType = @"";
    }
    [data setObject: adType forKey: @"ad_type"];
    [data setObject: adUnitId forKey: @"ad_unit_id"];
    if (adValue != nil) {
        [data setObject: adValue.value forKey: @"value"];
        [data setObject: @(adValue.precision) forKey: @"precision"];
        [data setObject: adValue.currencyCode forKey: @"currency_code"];
    }
    
    NSString *networkName = nil;
    if (responseInfo != nil) {
        if (responseInfo.extrasDictionary != nil) {
            NSString* placement = responseInfo.extrasDictionary[@"mediation_group_name"];
            if (placement != nil) {
                [data setObject: placement forKey: @"placement_name"];
            }
        }
        
        GADAdNetworkResponseInfo *adapterResponse = responseInfo.loadedAdNetworkResponseInfo;
        if (adapterResponse != nil) {
            networkName = adapterResponse.adSourceInstanceName;
            [data setObject: networkName forKey: @"network_name"];
        }
        
        NSMutableArray *waterfalls = [NSMutableArray array];
        for (GADAdNetworkResponseInfo *other in responseInfo.adNetworkInfoArray) {
            [waterfalls addObject: other.adSourceInstanceName];
        }
        [data setObject: waterfalls forKey: @"waterfall"];
    }
    
    [NeftaPlugin OnExternalMediationImpression: isClick provider: _mediationProvider data: data id: nil id2: id2];
}

NSString *_errorDomain = @"NeftaAMAdapter";
NSString *_idKey = @"parameter";

static NeftaPlugin *_plugin;

+ (GADVersionNumber)adSDKVersion {
    NSString *versionString = NeftaPlugin.Version;
    NSArray<NSString *> *versionComponents = [versionString componentsSeparatedByString:@"."];
    GADVersionNumber version = {
        versionComponents[0].integerValue,
        versionComponents[1].integerValue,
        versionComponents[2].integerValue};
    return version;
}

+ (GADVersionNumber)adapterVersion {
    GADVersionNumber version = {4, 4, 2};
    return version;
}

+ (void) setUpWithConfiguration:(GADMediationServerConfiguration *)configuration
             completionHandler:(GADMediationAdapterSetUpCompletionBlock)completionHandler {
    if (_plugin != nil) {
        completionHandler(nil);
        return;
    }

    _plugin = NeftaPlugin._instance;
    dispatch_async(dispatch_get_main_queue(), ^{
        if (_plugin == nil) {
            completionHandler([NSError errorWithDomain: _errorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
            return;
        }
        
        completionHandler(nil);
    });
}

- (void) loadBannerForAdConfiguration: (GADMediationBannerAdConfiguration *)adConfiguration
                   completionHandler: (GADMediationBannerLoadCompletionHandler)completionHandler {
    NSString* placementId = adConfiguration.credentials.settings[_idKey];
    if (placementId == nil || placementId.length == 0) {
        completionHandler(nil, [NSError errorWithDomain: _errorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
        return;
    }

    NeftaBanner *banner = [NeftaBanner Init: placementId listener: completionHandler errorDomain: _errorDomain];
    [banner Load];
}

- (void) loadInterstitialForAdConfiguration: (GADMediationInterstitialAdConfiguration *)adConfiguration
                         completionHandler: (GADMediationInterstitialLoadCompletionHandler)completionHandler {
    NSString* placementId = adConfiguration.credentials.settings[_idKey];
    if (placementId == nil || placementId.length == 0) {
        completionHandler(nil, [NSError errorWithDomain: _errorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
        return;
    }
    
    NeftaInterstitial *interstitial = [NeftaInterstitial Init: placementId listener: completionHandler errorDomain: _errorDomain];
    interstitial.extras = adConfiguration.extras;
    [interstitial Load];
}

- (void) loadRewardedAdForAdConfiguration: (GADMediationRewardedAdConfiguration *)adConfiguration
                       completionHandler: (GADMediationRewardedLoadCompletionHandler)completionHandler {
    NSString* placementId = adConfiguration.credentials.settings[_idKey];
    if (placementId == nil || placementId.length == 0) {
        completionHandler(nil, [NSError errorWithDomain: _errorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
        return;
    }
    
    NeftaRewarded *rewaded = [NeftaRewarded Init: placementId listener: completionHandler errorDomain: _errorDomain];
    rewaded.extras = adConfiguration.extras;
    [rewaded Load];
}

- (void) loadNativeAdForAdConfiguration: (GADMediationNativeAdConfiguration *)adConfiguration
                     completionHandler: (GADMediationNativeLoadCompletionHandler)completionHandler {
}

+ (NSError *) NLoadToAdapterError: (NError *)error {
    GADErrorCode code = GADErrorInternalError;
    if (error._code == CodeRequest) {
        code = GADErrorInvalidRequest;
    } else if (error._code == CodeNetwork) {
        code = GADErrorNetworkError;
    } else if (error._code == CodeNoFill) {
        code = GADErrorNoFill;
    } else if (error._code == CodeTimeout) {
        code = GADErrorTimeout;
    } else if (error._code == CodeResponse) {
        code = GADErrorInvalidArgument;
    }
    NSDictionary *userInfo = @ { NSLocalizedDescriptionKey: error._message };
    return [NSError errorWithDomain: _errorDomain code: error._code userInfo: userInfo];
}

@end
