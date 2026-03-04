//
//  NetworkConfig.swift
//  MaxIntegration
//
//  Created by Tomaz Treven on 9. 1. 26.
//

import UIKit

@objc public class NetworkConfig : NSObject {
    
    static var _title: String?
    static var _onClick: (() -> Void)? = nil
    static var _onReward: (() -> Void)? = nil
    static var _onClose: (() -> Void)? = nil
    
    @objc public static func Open(title: String,
                                   viewController: UIViewController,
                                   onShow: @escaping () -> Void,
                                   onClick: @escaping () -> Void,
                                   onReward: (() -> Void)? = nil,
                                   onClose: @escaping () -> Void) {
        
        _title = title
        _onClick = onClick
        _onReward = onReward
        _onClose = onClose
        
        onShow()
        
        let adViewController = AdViewController()
        adViewController.modalPresentationStyle = .fullScreen
        viewController.present(adViewController, animated: false, completion: nil)
    }
}

public class AdViewController : UIViewController {
    
    private var delayedTask: DispatchWorkItem?
    
    init() {
        super.init(nibName: nil, bundle: nil)
    }
    
    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
    }
    
    public override func viewDidLoad() {
        view.backgroundColor = .white
                
        let screenBounds = UIScreen.main.bounds
        
        let titleLabel = UILabel()
        titleLabel.text = NetworkConfig._title!
        titleLabel.font = UIFont.systemFont(ofSize: 24, weight: .bold)
        titleLabel.textAlignment = .center
        titleLabel.sizeToFit()
        let labelWidth: CGFloat = 300
        titleLabel.frame = CGRect(
            x: (screenBounds.width - labelWidth) / 2,
            y: 80 + view.safeAreaInsets.top,
            width: labelWidth,
            height: titleLabel.frame.height
        )
        view.addSubview(titleLabel)
    
        let closeButton = UIButton(type: .system)
        closeButton.setTitle("X", for: .normal)
        closeButton.titleLabel?.font = UIFont.systemFont(ofSize: 18)
        closeButton.backgroundColor = .systemRed
        closeButton.setTitleColor(.white, for: .normal)
        closeButton.layer.cornerRadius = 8
        closeButton.frame = CGRect(
            x: screenBounds.width - 80,
            y: 40 + view.safeAreaInsets.top,
            width: 40,
            height: 40
        )
        
        view.addSubview(closeButton)
        
        let centerButton = UIButton(type: .system)
        centerButton.setTitle("Ad", for: .normal)
        centerButton.titleLabel?.font = UIFont.systemFont(ofSize: 28)
        centerButton.backgroundColor = .systemBlue
        centerButton.setTitleColor(.white, for: .normal)
        centerButton.layer.cornerRadius = 16
        centerButton.frame = CGRect(
            x: (screenBounds.width - 300) / 2,
            y: (screenBounds.height - 400) / 2,
            width: 300,
            height: 400
        )
        
        view.addSubview(centerButton)
        
        closeButton.addTarget(self, action: #selector(closeTapped), for: .touchUpInside)
        centerButton.addTarget(self, action: #selector(centerTapped), for: .touchUpInside)
    }
    
    @objc private func closeTapped() {
        NetworkConfig._onClose!()
        dismiss(animated: false, completion: nil)
    }
    
    @objc private func centerTapped() {
        NetworkConfig._onClick!()
    }
    
    public override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)
        
        delayedTask?.cancel()
        
        let task = DispatchWorkItem { [weak self] in
            guard let self = self else { return }
            if self.isBeingDismissed || self.isMovingFromParent || self.view.window == nil {
                return
            }
            if let onReward = NetworkConfig._onReward {
                onReward()
            }
        }
        delayedTask = task
        DispatchQueue.main.asyncAfter(deadline: .now() + 3.0, execute: task)
    }
    
    public override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        delayedTask?.cancel()
        delayedTask = nil
    }
}
