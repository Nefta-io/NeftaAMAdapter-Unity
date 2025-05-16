#import "NeftaAdapter.h"
#import "NeftaBanner.h"
#import "NeftaInterstitial.h"
#import "NeftaRewarded.h"

@implementation NeftaAdapter

+ (Class<GADAdNetworkExtras>)networkExtrasClass {
    return [GADNeftaExtras class];
}

+(void) OnExternalMediationRequestLoad:(AdType)adType recommendedAdUnitId:(NSString *)recommendedAdUnitId calculatedFloorPrice:(double)calculatedFloorPrice adUnitId:(NSString *)adUnitId {
    [NeftaPlugin OnExternalMediationRequest: @"am" adType: adType recommendedAdUnitId: recommendedAdUnitId requestedFloorPrice: -1 calculatedFloorPrice: calculatedFloorPrice adUnitId: adUnitId revenue: -1 precision: @"" status: 1];
}

+(void) OnExternalMediationRequestFail:(AdType)adType recommendedAdUnitId:(NSString *)recommendedAdUnitId calculatedFloorPrice:(double)calculatedFloorPrice adUnitId:(NSString *)adUnitId error:(NSError *)error {
    int status = 0;
    if (error != nil && (error.code == GADErrorNoFill || error.code == GADErrorMediationNoFill)) {
        status = 2;
    }
    [NeftaPlugin OnExternalMediationRequest: @"am" adType: adType recommendedAdUnitId: recommendedAdUnitId requestedFloorPrice: -1 calculatedFloorPrice: calculatedFloorPrice adUnitId: adUnitId revenue: -1 precision: nil status: status];
}

+(void) OnExternalMediationRequestLoad:(AdType)adType requestedFloorPrice:(double)requestedFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice adUnitId:(NSString *)adUnitId {
    [NeftaPlugin OnExternalMediationRequest: @"am" adType: adType recommendedAdUnitId: nil requestedFloorPrice: requestedFloorPrice calculatedFloorPrice: calculatedFloorPrice adUnitId: adUnitId revenue: -1 precision: @"" status: 1];
}

+(void) OnExternalMediationRequestFail:(AdType)adType requestedFloorPrice:(double)requestedFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice adUnitId:(NSString *)adUnitId error:(NSError *)error {
    int status = 0;
    if (error != nil && (error.code == GADErrorNoFill || error.code == GADErrorMediationNoFill)) {
        status = 2;
    }
    [NeftaPlugin OnExternalMediationRequest: @"am" adType: adType recommendedAdUnitId: nil requestedFloorPrice: requestedFloorPrice calculatedFloorPrice: calculatedFloorPrice adUnitId: adUnitId revenue: -1 precision: nil status: status];
}

+(void) OnExternalMediationImpression:(GADAdValue*)adValue {
    NSMutableDictionary *data = [NSMutableDictionary dictionary];
    [data setObject: adValue.value forKey: @"value"];
    [data setObject: @(adValue.precision) forKey: @"precision"];
    [data setObject: adValue.currencyCode forKey: @"currency_code"];

    [NeftaPlugin OnExternalMediationImpression: @"am" data: data];
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
    GADVersionNumber version = {2, 2, 3};
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
        code =  GADErrorNetworkError;
    } else if (error._code == CodeNoFill) {
        code =  GADErrorNoFill;
    } else if (error._code == CodeTimeout) {
        code =  GADErrorTimeout;
    } else if (error._code == CodeResponse) {
        code =  GADErrorReceivedInvalidResponse;
    }
    NSDictionary *userInfo = @ { NSLocalizedDescriptionKey: error._message };
    return [NSError errorWithDomain: _errorDomain code: error._code userInfo: userInfo];
}

@end

#ifdef __cplusplus
extern "C" {
#endif
    typedef void (*OnBehaviourInsight)(int requestId, const char *behaviourInsight);
    
    void EnableLogging(bool enable);
    void NeftaPlugin_Init(const char *appId, OnBehaviourInsight onBehaviourInsight);
    void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload);
    const char * NeftaPlugin_GetNuid(bool present);
    void NeftaPlugin_SetContentRating(const char *rating);
    void NeftaPlugin_GetBehaviourInsight(int requestId, const char *insights);
#ifdef __cplusplus
}
#endif

void NeftaPlugin_EnableLogging(bool enable) {
    [NeftaPlugin EnableLogging: enable];
}

void NeftaPlugin_Init(const char *appId, OnBehaviourInsight onBehaviourInsight) {
    _plugin = [NeftaPlugin InitWithAppId: [NSString stringWithUTF8String: appId]];
    _plugin.OnBehaviourInsightAsString = ^void(NSInteger requestId, NSString * _Nonnull behaviourInsight) {
        const char *cBI = [behaviourInsight UTF8String];
        onBehaviourInsight((int)requestId, cBI);
    };
}

void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload) {
    NSString *n = name ? [NSString stringWithUTF8String: name] : nil;
    NSString *cp = customPayload ? [NSString stringWithUTF8String: customPayload] : nil;
    [_plugin RecordWithType: type category: category subCategory: subCategory name: n value: value customPayload: cp];
}

const char * NeftaPlugin_GetNuid(bool present) {
    const char *string = [[_plugin GetNuidWithPresent: present] UTF8String];
    char *returnString = (char *)malloc(strlen(string) + 1);
    strcpy(returnString, string);
    return returnString;
}

void NeftaPlugin_SetContentRating(const char *rating) {
    [_plugin SetContentRatingWithRating: [NSString stringWithUTF8String: rating]];
}

void NeftaPlugin_GetBehaviourInsight(int requestId, const char *insights) {
    [_plugin GetBehaviourInsightBridge: requestId string: [NSString stringWithUTF8String: insights]];
}
