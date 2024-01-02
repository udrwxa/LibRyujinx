//
//  LibraryView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI

struct LibraryView: View {
    @EnvironmentObject var settings: Settings

    @State var search: String = ""

    private let gridLayout = [GridItem(.adaptive(minimum: 100))]

    var body: some View {
        NavigationStack {
            ScrollView {
                LazyVGrid(columns: gridLayout, spacing: 10) {
                    GameView(imageName: "Racing", title: "SEGA AGES VIRTUA RACING")
                    GameView(imageName: "Mania", title: "Sonic Mania")
                    GameView(imageName: "BOTW", title: "The Legend of Zelda: Breath of the Wild")
                    GameView(imageName: "TOTK", title: "The Legend of Zelda: Tears of the Kingdom")
                    GameView(imageName: "Undertale", title: "Undertale")
                }
                .padding(.horizontal)
            }
        }
        .searchable(text: $search)
    }
}

#Preview {
    LibraryView()
}
