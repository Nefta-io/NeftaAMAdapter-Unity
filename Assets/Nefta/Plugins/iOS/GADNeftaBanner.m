#import "GADNeftaBanner.h"
#import "GADNeftaAdapter.h"

static NSString* _lastCreativeId;
static NSString* _lastAuctionId;

@implementation NeftaBanner
+ (instancetype)Init:(NSString *)id listener:(nonnull GADMediationBannerLoadCompletionHandler)listener errorDomain:(NSString *)errorDomain {
    NeftaBanner *instance = [[self alloc] init];
    instance.banner = [[NBanner alloc] initWithId: id position: PositionNone];
    instance.listener = listener;
    instance.errorDomain = errorDomain;
    instance.banner._listener = instance;
    return instance;
}

- (void)Load {
    [_banner Load];
}

- (void)OnLoadFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    _listener(nil, [GADNeftaAdapter NLoadToAdapterError: error]);
}

- (void)OnLoadWithAd:(NAd * _Nonnull)ad width:(NSInteger)width height:(NSInteger)height {
    ((NBanner *)ad)._onRemove = ^(NBanner * _Nonnull banner) {
        [banner Close];
    };
    (void)[_banner GracefulShow: nil];
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
    [_adEventDelegate reportImpression];
}

- (void)OnClickWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate reportClick];
}

- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
}

- (nonnull UIView *)view {
    return [_banner GetView];
}

+ (NSString*) GetLastAuctionId {
    return _lastAuctionId;
}
+ (NSString*) GetLastCreativeId {
    return _lastCreativeId;
}
@end
