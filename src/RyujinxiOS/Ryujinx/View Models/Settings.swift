//
//  Settings.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 01/01/2024.
//

import Foundation

class Settings: ObservableObject {
    @Published public var settings: RyujinxSettings {
        didSet { settings.encode() }
    }

    public init() {
        self.settings = RyujinxSettings.decode()
    }
}
