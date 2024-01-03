//
//  GameView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI

struct GameView: View {
    @EnvironmentObject var games: Games

    @State var game: Game
    @State var icon: Image = Image("Icon_NSP")

    var body: some View {
        VStack {
            icon
                .resizable()
                .frame(width: 80, height: 80)
                .clipShape(RoundedRectangle(cornerRadius: 5))
                .overlay(
                    RoundedRectangle(cornerRadius: 5)
                        .stroke(.regularMaterial, lineWidth: 1)
                )
            Text(game.titleName)
                .multilineTextAlignment(.center)
                .lineLimit(2, reservesSpace: true)
                .truncationMode(.tail)
        }
        .padding(5)
        .onAppear {
            self.icon = game.placeholderForFiletype()

            if let icon = game.icon {
                self.icon = icon
            }
        }
        .contextMenu {
            Button(role: .destructive) {
                do {
                    try FileManager.default.removeItem(at: game.containerFolder)
                    games.games.removeAll(where: { $0 == game })
                } catch {
                    print(error)
                }
            } label: {
                Label("Delete Game", systemImage: "trash")
            }
        }
    }
}

#Preview {
    GameView(game: Game(containerFolder: URL(fileURLWithPath: ""),
                        fileType: .nsp, 
                        titleName: "Legend of Zelda: Breath of the Wild",
                        titleId: "",
                        developer: "",
                        version: "",
                        icon: Image("Icon_NSP")))
}
