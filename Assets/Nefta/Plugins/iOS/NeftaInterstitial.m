#import "NeftaInterstitial.h"

@implementation NeftaInterstitial

+ (instancetype)Init:(NSString *)id listener:(nonnull GADMediationInterstitialLoadCompletionHandler)listener errorDomain:(NSString *_Nonnull)errorDomain {
    NeftaInterstitial *instance = [[self alloc] init];
    instance.interstitial = [[NInterstitial alloc] initWithId: id];
    instance.listener = listener;
    instance.errorDomain = errorDomain;
    instance.interstitial._listener = instance;
    return instance;
}

- (void)Load {
    [_interstitial Load];
}

- (void)presentFromViewController:(nonnull UIViewController *)viewController {
    int status = (int) [_interstitial CanShow];
    if (status != NAd.Ready) {
        NSError *showError = [NSError errorWithDomain: @"NeftaAMAdapter"
                                             code: status
                                         userInfo: @{NSLocalizedDescriptionKey : @"Ad not ready."}];
        [_adEventDelegate didFailToPresentWithError:showError];
        return;
    }

    [NeftaPlugin._instance PrepareRendererWithViewController: viewController];
    if (_extras != nil && _extras.muteAudio) {
        [_interstitial Mute: true];
    }
    [_interstitial Show];
}

- (void)OnLoadFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    _listener(nil, [NSError errorWithDomain: error._message code: error._code userInfo: nil]);
}

- (void)OnLoadWithAd:(NAd * _Nonnull)ad width:(NSInteger)width height:(NSInteger)height {
    _adEventDelegate = _listener(self, nil);
}

- (void)OnShowFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    NSDictionary *userInfo = @ { NSLocalizedDescriptionKey: error._message };
    [_adEventDelegate didFailToPresentWithError: [NSError errorWithDomain: @"NeftaAMAdapter" code: error._code userInfo: userInfo]];
}

- (void)OnShowWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate willPresentFullScreenView];
}

- (void)OnClickWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate reportClick];
}

- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate didDismissFullScreenView];
}

@end


