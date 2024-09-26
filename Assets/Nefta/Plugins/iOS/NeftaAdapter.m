#import "NeftaAdapter.h"
#import "AdBannerNeftaRequest.h"
#import "InterstitialNeftaRequest.h"
#import "RewardedVideoNeftaRequest.h"

@implementation NeftaAdapter

+ (Class<GADAdNetworkExtras>)networkExtrasClass {
    return [NeftaExtras class];
}

NSString *_errorDomain = @"NeftaAMAdapter";
NSString *_idKey = @"parameter";

static NeftaPlugin *_plugin;
static NSMutableArray *_requests;

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
    GADVersionNumber version = {1, 3, 0};
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
        [NeftaAdapter Init];
        
        completionHandler(nil);
    });
}

+ (void) Init {
    _plugin.OnLoadFail = ^(Placement *placement, NSString *error) {
        for (int i = 0; i < _requests.count; i++) {
            id<NeftaRequest> r = _requests[i];
            if ([r._placementId isEqualToString: placement._id] && r._state == 0) {
                [r OnLoadFail: error];
                [_requests removeObject: r];
                return;
            }
        }
    };
    _plugin.OnLoad = ^(Placement *placement, NSInteger width, NSInteger height) {
        for (int i = 0; i < _requests.count; i++) {
            id<NeftaRequest> r = _requests[i];
            if ([r._placementId isEqualToString: placement._id] && r._state == 0) {
                r._state = 1;
                [r OnLoad: placement];
                return;
            }
        }
    };
    _plugin.OnShow = ^(Placement *placement) {
        for (int i = 0; i < _requests.count; i++) {
            id<NeftaRequest> r = _requests[i];
            if ([r._placementId isEqualToString: placement._id] && r._state == 1) {
                r._state = 2;
                [r OnShow];
                return;
            }
        }
    };
    _plugin.OnClick = ^(Placement *placement) {
        for (int i = 0; i < _requests.count; i++) {
            id<NeftaRequest> r = _requests[i];
            if ([r._placementId isEqualToString: placement._id] && r._state == 2) {
                [r OnClick];
                return;
            }
        }
    };
    _plugin.OnReward = ^(Placement *placement) {
        for (int i = 0; i < _requests.count; i++) {
            id<NeftaRequest> r = _requests[i];
            if ([r._placementId isEqualToString: placement._id] && r._state == 2) {
                [r OnRewarded];
                return;
            }
        }
    };
    _plugin.OnClose = ^(Placement *placement) {
        for (int i = 0; i < _requests.count; i++) {
            id<NeftaRequest> r = _requests[i];
            if ([r._placementId isEqualToString: placement._id] && r._state == 2) {
                [r OnClose];
                [_requests removeObject: r];
                return;
            }
        }
    };
    
    _requests = [NSMutableArray array];
    
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

    AdBannerNeftaRequest *request = [AdBannerNeftaRequest Init: self placementId: placementId callback: completionHandler];
    [_requests addObject: request];

    UIApplication *application = [UIApplication sharedApplication];
    UIWindow *keyWindow = application.keyWindow;
    [_plugin PrepareRendererWithViewController: keyWindow.rootViewController];
    
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
    
    InterstitialNeftaRequest *request = [InterstitialNeftaRequest Init: self placementId: placementId callback: completionHandler];
    [_requests addObject: request];
    
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
    
    RewardedVideoNeftaRequest *request = [RewardedVideoNeftaRequest Init: self placementId: placementId callback: completionHandler];
    NeftaExtras *extras = adConfiguration.extras;
    if (extras != nil) {
        request.muteAudio = extras.muteAudio;
    }
    [_requests addObject: request];
    
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
    void NeftaPlugin_Record(void *instance, int type, int category, int subCategory, const char *name, long value, const char *customPayload);
    const char * NeftaPlugin_GetNuid(void *instance, bool present);
#ifdef __cplusplus
}
#endif

void NeftaPlugin_EnableLogging(bool enable) {
    [NeftaPlugin EnableLogging: enable];
}

void * NeftaPlugin_Init(const char *appId) {
    _plugin = [NeftaPlugin InitWithAppId: [NSString stringWithUTF8String: appId]];
    [NeftaAdapter Init];
    return (__bridge_retained void *)_plugin;
}

void NeftaPlugin_Record(void *instance, int type, int category, int subCategory, const char *name, long value, const char *customPayload) {
    [_plugin RecordWithType: type category: category subCategory: subCategory name: [NSString stringWithUTF8String: name] value: value customPayload: [NSString stringWithUTF8String: customPayload]];
}

const char * NeftaPlugin_GetNuid(void *instance, bool present) {
    const char *string = [[_plugin GetNuidWithPresent: present] UTF8String];
    char *returnString = (char *)malloc(strlen(string) + 1);
    strcpy(returnString, string);
    return returnString;
}
