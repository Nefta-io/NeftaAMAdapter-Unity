#import "GADNeftaRewarded.h"
#import "GADNeftaAdapter.h"

static NSString* _lastCreativeId;
static NSString* _lastAuctionId;

@implementation NeftaRewarded

+ (instancetype)Init:(NSString *)id listener:(nonnull GADMediationRewardedLoadCompletionHandler)listener errorDomain:(NSString *_Nonnull)errorDomain {
    NeftaRewarded *instance = [[self alloc] init];
    instance.rewarded = [[NRewarded alloc] initWithId: id];
    instance.listener = listener;
    instance.rewarded._listener = instance;
    return instance;
}

- (void)Load {
    [_rewarded Load];
}

- (void)presentFromViewController:(nonnull UIViewController *)viewController {
    int status = (int) [_rewarded CanShow];
    if (status != NAd.Ready) {
        NSError *showError = [NSError errorWithDomain: _errorDomain
                                                 code: status
                                             userInfo: @{NSLocalizedDescriptionKey : @"Ad not ready."}];
        [_adEventDelegate didFailToPresentWithError: showError];
        return;
    }

    if (_extras != nil && _extras.muteAudio) {
        [_rewarded Mute: true];
    }
    [_rewarded Show: viewController];
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

- (void)OnRewardWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate didRewardUser];
}

- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate didDismissFullScreenView];
}


- (void)didCompleteRewardedVideoForAdWithAd:(NAd * _Nonnull)ad { 
    
}

- (void)didStartRewardedVideoForAdWithAd:(NAd * _Nonnull)ad { 
    
}

+ (NSString*) GetLastAuctionId {
    return _lastAuctionId;
}
+ (NSString*) GetLastCreativeId {
    return _lastCreativeId;
}
@end
