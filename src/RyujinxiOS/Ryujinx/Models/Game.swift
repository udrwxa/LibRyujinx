//
//  Game.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 02/01/2024.
//

import SwiftUI
import UniformTypeIdentifiers

public struct Game: Identifiable, Equatable {
    public var id = UUID()

    var containerFolder: URL
    var fileType: UTType

    var titleName: String
    var titleId: String
    var developer: String
    var version: String
    var icon: Image?

    public func placeholderForFiletype() -> Image {
        switch fileType {
        case .nca:
            Image("Icon_NCA")
        case .nro:
            Image("Icon_NRO")
        case .nso:
            Image("Icon_NSO")
        case .nsp:
            Image("Icon_NSP")
        case .xci:
            Image("Icon_XCI")
        default:
            Image("Icon_NSP")
        }
    }
}
