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
    @State var games: [Game] = []

    private let gridLayout = [GridItem(.adaptive(minimum: 100))]

    var body: some View {
        NavigationStack {
            ScrollView {
                LazyVGrid(columns: gridLayout, spacing: 10) {
                    ForEach(games, id: \.id) { game in
                        GameView(game: game)
                    }
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
