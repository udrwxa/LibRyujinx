//
//  Games.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 03/01/2024.
//

import Foundation

class Games: ObservableObject {
    @Published public var games: [Game]

    public init() {
        self.games = []
    }
}
