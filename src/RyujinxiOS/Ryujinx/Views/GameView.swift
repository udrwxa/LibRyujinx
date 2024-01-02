//
//  GameView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI

struct GameView: View {
    @State var game: Game
    @State var icon: Image = Image("TOTK")

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
            if let icon = game.icon {
                self.icon = icon
            }
        }
    }
}

#Preview {
    GameView(game: Game(titleName: "Legend of Zelda: Breath of the Wild",
                        titleId: "",
                        developer: "",
                        version: "",
                        icon: Image("BOTW")))
}
