//
//  Game.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 02/01/2024.
//

import SwiftUI

public struct Game: Identifiable, Equatable {
    public var id = UUID()

    var containerFolder: URL
    var titleName: String
    var titleId: String
    var developer: String
    var version: String
    var icon: Image?
}
