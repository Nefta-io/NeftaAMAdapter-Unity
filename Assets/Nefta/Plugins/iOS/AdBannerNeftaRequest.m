#import "NeftaRequest.h"
#import "AdBannerNeftaRequest.h"

@implementation AdBannerNeftaRequest {
    __weak id<GADMediationBannerAdEventDelegate> _adEventDelegate;
}

@synthesize _adapter;
@synthesize _placement;
@synthesize _placementId;
@synthesize _state;

+ (instancetype)Init:(NeftaAdapter *)adapter placementId:(NSString *)placementId callback:(nonnull GADMediationBannerLoadCompletionHandler)callback {
    AdBannerNeftaRequest *instance = [[self alloc] init];
    instance._adapter = adapter;
    instance.callback = callback;
    instance._placementId = placementId;
    instance._state = 0;
    return instance;
}

- (void)OnLoadFail:(NSString *)error {
    _callback(nil, [NSError errorWithDomain: _adapter.ErrorDomain code: NeftaAdapterErrorCodeInvalidServerParameters userInfo: nil]);
}

- (void)OnLoad:(Placement *)placement {
    _placement = placement;
    [_adapter.Plugin ShowMainWithId: _placement._id];
    _adEventDelegate = _callback(self, nil);
}

- (void)OnShow:(NSInteger)width height:(NSInteger)height {
    [_adEventDelegate reportImpression];
}

- (void)OnClick {
    [_adEventDelegate reportClick];
}

- (void)OnRewarded {
    
}

- (void)OnClose {
    
}

- (nonnull UIView *)view {
    return [_adapter.Plugin GetViewForPlacement: _placement show: false];
}


@end
