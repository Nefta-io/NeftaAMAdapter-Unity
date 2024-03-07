#import "NeftaAdapter.h"
#import <NeftaSDK/NeftaSDK-Swift.h>
#import "AdBannerNeftaRequest.h"
#import "InterstitialNeftaRequest.h"
#import "RewardedVideoNeftaRequest.h"
#import "NeftaExtras.h"

@implementation NeftaAdapter

+ (Class<GADAdNetworkExtras>)networkExtrasClass {
    return [NeftaExtras class];
}

NSString *_errorDomain = @"NeftaAMAdapter";
NSString *_idKey = @"parameter";

static NeftaPlugin_iOS *_plugin;
static NSMutableDictionary<NSString *, id<NeftaRequest>> *_requests;

+ (GADVersionNumber)adSDKVersion {
    NSString *versionString = NeftaPlugin_iOS.Version;
    NSArray<NSString *> *versionComponents = [versionString componentsSeparatedByString:@"."];
    GADVersionNumber version = {
        versionComponents[0].integerValue,
        versionComponents[1].integerValue,
        versionComponents[2].integerValue};
    return version;
}

+ (GADVersionNumber)adapterVersion {
    GADVersionNumber version = {1, 0, 0};
    return version;
}

+ (void) setUpWithConfiguration:(GADMediationServerConfiguration *)configuration
             completionHandler:(GADMediationAdapterSetUpCompletionBlock)completionHandler {
    if (_plugin != nil) {
        completionHandler(nil);
        return;
    }

    _plugin = NeftaPlugin_iOS._instance;
    dispatch_async(dispatch_get_main_queue(), ^{
        if (_plugin == nil) {
            completionHandler([NSError errorWithDomain: _errorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
        }
        [NeftaAdapter Init];
        
        completionHandler(nil);
    });
}

+ (void) Init {
    _plugin.OnLoadFail = ^(Placement *placement, NSString *error) {
        id<NeftaRequest> request = _requests[placement._id];
        [request OnLoadFail: error];
        [_requests removeObjectForKey: placement._id];
    };
    _plugin.OnLoad = ^(Placement *placement) {
        id<NeftaRequest> request = _requests[placement._id];
        [request OnLoad: placement];
    };
    _plugin.OnShow = ^(Placement *placement, NSInteger width, NSInteger height) {
        id<NeftaRequest> request = _requests[placement._id];
        [request OnShow: width height: height];
    };
    _plugin.OnClick = ^(Placement *placement) {
        id<NeftaRequest> request = _requests[placement._id];
        [request OnClick];
    };
    _plugin.OnReward = ^(Placement *placement) {
        id<NeftaRequest> request = _requests[placement._id];
        [request OnRewarded];
    };
    _plugin.OnClose = ^(Placement *placement) {
        id<NeftaRequest> request = _requests[placement._id];
        [request OnClose];
        [_requests removeObjectForKey: placement._id];
    };
    
    _requests = [[NSMutableDictionary alloc] init];
    
    [_plugin EnableAds: true];
}

- (void) loadBannerForAdConfiguration: (GADMediationBannerAdConfiguration *)adConfiguration
                   completionHandler: (GADMediationBannerLoadCompletionHandler)completionHandler {
    NSString* placementId = adConfiguration.credentials.settings[_idKey];
    if (placementId == nil || placementId.length == 0) {
        completionHandler(nil, [NSError errorWithDomain: _errorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
        return;
    }
    
    _ErrorDomain = _errorDomain;
    _Plugin = _plugin;

    AdBannerNeftaRequest *request = [AdBannerNeftaRequest Init: self callback: completionHandler];
    _requests[placementId] = request;

    UIApplication *application = [UIApplication sharedApplication];
    UIWindow *keyWindow = application.keyWindow;
    UIView *rootView = keyWindow.rootViewController.view;
    [_plugin PrepareRendererWithView: rootView];
    
    [_plugin LoadWithId: placementId];
}

- (void) loadInterstitialForAdConfiguration: (GADMediationInterstitialAdConfiguration *)adConfiguration
                         completionHandler: (GADMediationInterstitialLoadCompletionHandler)completionHandler {
    NSString* placementId = adConfiguration.credentials.settings[_idKey];
    if (placementId == nil || placementId.length == 0) {
        completionHandler(nil, [NSError errorWithDomain: _errorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
        return;
    }
    
    _ErrorDomain = _errorDomain;
    _Plugin = _plugin;
    
    InterstitialNeftaRequest *request = [InterstitialNeftaRequest Init: self callback: completionHandler];
    _requests[placementId] = request;
    
    [_plugin LoadWithId: placementId];
}

- (void) loadRewardedAdForAdConfiguration: (GADMediationRewardedAdConfiguration *)adConfiguration
                       completionHandler: (GADMediationRewardedLoadCompletionHandler)completionHandler {
    NSString* placementId = adConfiguration.credentials.settings[_idKey];
    if (placementId == nil || placementId.length == 0) {
        completionHandler(nil, [NSError errorWithDomain: _errorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
        return;
    }
    
    _ErrorDomain = _errorDomain;
    _Plugin = _plugin;
    
    RewardedVideoNeftaRequest *request = [RewardedVideoNeftaRequest Init: self callback: completionHandler];
    NeftaExtras *extras = adConfiguration.extras;
    if (extras != nil) {
        request.muteAudio = extras.muteAudio;
    }
    _requests[placementId] = request;
    
    [_plugin LoadWithId: placementId];
}

- (void) loadNativeAdForAdConfiguration: (GADMediationNativeAdConfiguration *)adConfiguration
                     completionHandler: (GADMediationNativeLoadCompletionHandler)completionHandler {
}

@end

#ifdef __cplusplus
extern "C" {
#endif
    void EnableLogging(bool enable);
    void * NeftaPlugin_Init(const char *appId);
    void NeftaPlugin_Record(void *instance, const char *event);
#ifdef __cplusplus
}
#endif

void NeftaPlugin_EnableLogging(bool enable) {
    [NeftaPlugin_iOS EnableLogging: enable];
}

void * NeftaPlugin_Init(const char *appId) {
    _plugin = [NeftaPlugin_iOS InitWithAppId: [NSString stringWithUTF8String: appId]];
    [NeftaAdapter Init];
    return (__bridge_retained void *)_plugin;
}

void NeftaPlugin_Record(void *instance, const char *event) {
    [_plugin RecordWithEvent: [NSString stringWithUTF8String: event]];
}

const char * NeftaPlugin_ShowNuid(void *instance) {
    const char *string = [[_plugin ShowNuid] UTF8String];
    char *returnString = (char *)malloc(strlen(string) + 1);
    strcpy(returnString, string);
    return returnString;
}