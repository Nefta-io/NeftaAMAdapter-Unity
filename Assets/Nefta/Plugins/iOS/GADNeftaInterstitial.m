#import "GADNeftaInterstitial.h"
#import "GADNeftaAdapter.h"

static NSString* _lastCreativeId;
static NSString* _lastAuctionId;

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
        NSError *showError = [NSError errorWithDomain: _errorDomain
                                             code: status
                                         userInfo: @{NSLocalizedDescriptionKey : @"Ad not ready."}];
        [_adEventDelegate didFailToPresentWithError:showError];
        return;
    }

    if (_extras != nil && _extras.muteAudio) {
        [_interstitial Mute: true];
    }
    [_interstitial Show: viewController];
}

- (void)OnLoadFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    _listener(nil, [GADNeftaAdapter NLoadToAdapterError: error]);
}

- (void)OnLoadWithAd:(NAd * _Nonnull)ad width:(NSInteger)width height:(NSInteger)height {
    _adEventDelegate = _listener(self, nil);
}

- (void)OnShowFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    NSError *showError = [NSError errorWithDomain: _errorDomain
                                             code: 0
                                         userInfo: @{NSLocalizedDescriptionKey : @"Show failed."}];
    [_adEventDelegate didFailToPresentWithError: showError];
}

- (void)OnShowWithAd:(NAd * _Nonnull)ad {
    _lastAuctionId = ad._bid._auctionId;
    _lastCreativeId = ad._bid._creativeId;
    [_adEventDelegate willPresentFullScreenView];
}

- (void)OnClickWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate reportClick];
}

- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate didDismissFullScreenView];
}

+ (NSString*) GetLastAuctionId {
    return _lastAuctionId;
}
+ (NSString*) GetLastCreativeId {
    return _lastCreativeId;
}
@end


