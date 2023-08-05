package org.ryujinx.android.views

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.wrapContentHeight
import androidx.compose.foundation.layout.wrapContentWidth
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AlertDialogDefaults
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.PointerEventType
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.compose.ui.window.Popup
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import compose.icons.CssGgIcons
import compose.icons.cssggicons.ToolbarBottom
import org.ryujinx.android.GameController
import org.ryujinx.android.GameHost
import org.ryujinx.android.Icons
import org.ryujinx.android.RyujinxNative
import org.ryujinx.android.viewmodels.MainViewModel
import org.ryujinx.android.viewmodels.QuickSettings
import org.ryujinx.android.viewmodels.SettingsViewModel
import kotlin.math.roundToInt

class MainView {
    companion object {
        @Composable
        fun Main(mainViewModel: MainViewModel) {
            val navController = rememberNavController()
            mainViewModel.navController = navController

            NavHost(navController = navController, startDestination = "home") {
                composable("home") { HomeViews.Home(mainViewModel.homeViewModel, navController) }
                composable("game") { GameView(mainViewModel) }
                composable("settings") {
                    SettingViews.Main(
                        SettingsViewModel(
                            navController,
                            mainViewModel.activity
                        )
                    )
                }
            }
        }

        @Composable
        fun GameView(mainViewModel: MainViewModel) {
            Box(modifier = Modifier.fillMaxSize()) {
                AndroidView(
                    modifier = Modifier.fillMaxSize(),
                    factory = { context ->
                        GameHost(context, mainViewModel)
                    }
                )
                GameOverlay(mainViewModel)
            }
        }

        @OptIn(ExperimentalMaterial3Api::class)
        @Composable
        fun GameOverlay(mainViewModel: MainViewModel) {
            Box(modifier = Modifier.fillMaxSize()) {
                GameStats(mainViewModel)

                val ryujinxNative = RyujinxNative()

                var showController = remember {
                    mutableStateOf(QuickSettings(mainViewModel.activity).useVirtualController)
                }
                var enableVsync = remember {
                    mutableStateOf(QuickSettings(mainViewModel.activity).enableVsync)
                }
                var showMore = remember {
                    mutableStateOf(false)
                }

                // touch surface
                Surface(color = Color.Transparent, modifier = Modifier
                    .fillMaxSize()
                    .padding(0.dp)
                    .pointerInput(Unit) {
                        awaitPointerEventScope {
                            while (true) {
                                val event = awaitPointerEvent()
                                if (!showController.value)
                                    continue

                                val change = event
                                    .component1()
                                    .firstOrNull()
                                change?.apply {
                                    val position = this.position

                                    when (event.type) {
                                        PointerEventType.Press -> {
                                            ryujinxNative.inputSetTouchPoint(
                                                position.x.roundToInt(),
                                                position.y.roundToInt()
                                            )
                                        }

                                        PointerEventType.Release -> {
                                            ryujinxNative.inputReleaseTouchPoint()

                                        }

                                        PointerEventType.Move -> {
                                            ryujinxNative.inputSetTouchPoint(
                                                position.x.roundToInt(),
                                                position.y.roundToInt()
                                            )

                                        }
                                    }
                                }
                            }
                        }
                    }) {
                }
                GameController.Compose(mainViewModel)
                Row(
                    modifier = Modifier
                        .align(Alignment.BottomCenter)
                        .padding(8.dp)
                ) {
                    IconButton(modifier = Modifier.padding(4.dp), onClick = {
                        showMore.value = true
                    }) {
                        Icon(
                            imageVector = CssGgIcons.ToolbarBottom,
                            contentDescription = "Open Panel"
                        )
                    }
                }

                if(showMore.value){
                    Popup(alignment = Alignment.BottomCenter, onDismissRequest = {showMore.value = false}) {
                        Surface(modifier = Modifier.padding(16.dp),
                            shape = MaterialTheme.shapes.medium) {
                            Row(modifier = Modifier.padding(8.dp)) {
                                IconButton(modifier = Modifier.padding(4.dp), onClick = {
                                    showMore.value = false
                                    showController.value = !showController.value
                                    mainViewModel.controller?.setVisible(showController.value)
                                }) {
                                    Icon(
                                        imageVector = Icons.videoGame(),
                                        contentDescription = "Toggle Virtual Pad"
                                    )
                                }
                                IconButton(modifier = Modifier.padding(4.dp), onClick = {
                                    showMore.value = false
                                    enableVsync.value = !enableVsync.value
                                    RyujinxNative().graphicsRendererSetVsync(enableVsync.value)
                                }) {
                                    Icon(
                                        imageVector = Icons.vSync(),
                                        tint = if(enableVsync.value) Color.Green else Color.Red,
                                        contentDescription = "Toggle VSync"
                                    )
                                }
                            }
                        }
                    }
                }

                var showBackNotice = remember {
                    mutableStateOf(false)
                }

                BackHandler {
                    showBackNotice.value = true
                }

                if (showBackNotice.value) {
                    AlertDialog(onDismissRequest = { showBackNotice.value = false }) {
                        Column {
                            Surface(
                                modifier = Modifier
                                    .wrapContentWidth()
                                    .wrapContentHeight(),
                                shape = MaterialTheme.shapes.large,
                                tonalElevation = AlertDialogDefaults.TonalElevation
                            ) {
                                Column {
                                    Column(
                                        modifier = Modifier
                                            .fillMaxWidth()
                                            .padding(16.dp)
                                    ) {
                                        Text(text = "Are you sure you want to exit the game?")
                                        Text(text = "All unsaved data will be lost!")
                                    }
                                    Row(
                                        horizontalArrangement = Arrangement.End,
                                        modifier = Modifier
                                            .fillMaxWidth()
                                            .padding(16.dp)
                                    ) {
                                        Button(onClick = {
                                            mainViewModel.closeGame()
                                        }, modifier = Modifier.padding(16.dp)) {
                                            Text(text = "Exit Game")
                                        }

                                        Button(onClick = {
                                            showBackNotice.value = false
                                        }, modifier = Modifier.padding(16.dp)) {
                                            Text(text = "Dismiss")
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        @Composable
        fun GameStats(mainViewModel: MainViewModel) {
            val fifo = remember {
                mutableStateOf(0.0)
            }
            val gameFps = remember {
                mutableStateOf(0.0)
            }
            val gameTime = remember {
                mutableStateOf(0.0)
            }

            Surface(
                modifier = Modifier.padding(16.dp),
                color = MaterialTheme.colorScheme.surface.copy(0.4f)
            ) {
                Column {
                    var gameTimeVal = 0.0
                    if (!gameTime.value.isInfinite())
                        gameTimeVal = gameTime.value
                    Text(text = "${String.format("%.3f", fifo.value)} %")
                    Text(text = "${String.format("%.3f", gameFps.value)} FPS")
                    Text(text = "${String.format("%.3f", gameTimeVal)} ms")
                }
            }

            mainViewModel.setStatStates(fifo, gameFps, gameTime)
        }
    }
}
