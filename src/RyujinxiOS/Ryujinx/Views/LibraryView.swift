//
//  LibraryView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI

struct LibraryView: View {
    @State var search: String = ""

    private let gridLayout = [GridItem(.adaptive(minimum: 100, maximum: .infinity))]

    var body: some View {
        NavigationStack {
            ScrollView {
                LazyVGrid(columns: gridLayout, alignment: .leading) {
                    GameView(imageName: "Racing", title: "SEGA AGES VIRTUA RACING")
                    GameView(imageName: "Mania", title: "Sonic Mania")
                    GameView(imageName: "BOTW", title: "The Legend of Zelda: Breath of the Wild")
                    GameView(imageName: "TOTK", title: "The Legend of Zelda: Tears of the Kingdom")
                    GameView(imageName: "Undertale", title: "Undertale")
                }
            }
        }
        .searchable(text: $search)
    }
}

#Preview {
    LibraryView()
}
