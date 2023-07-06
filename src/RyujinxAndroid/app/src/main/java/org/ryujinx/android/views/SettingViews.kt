package org.ryujinx.android.views

import android.annotation.SuppressLint
import androidx.activity.compose.BackHandler
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.animateColor
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.MutableTransitionState
import androidx.compose.animation.core.animateDp
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.tween
import androidx.compose.animation.core.updateTransition
import androidx.compose.animation.expandVertically
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.shrinkVertically
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import org.ryujinx.android.viewmodels.SettingsViewModel

class SettingViews {
    companion object {
        const val EXPANSTION_TRANSITION_DURATION = 450

        @OptIn(ExperimentalMaterial3Api::class)
        @Composable
        fun Main(settingsViewModel: SettingsViewModel) {
            var loaded = remember {
                mutableStateOf(false)
            }

            var isHostMapped = remember {
                mutableStateOf(false)
            }
            var useNce = remember {
                mutableStateOf(false)
            }
            var enableVsync = remember {
                mutableStateOf(false)
            }
            var enableDocked = remember {
                mutableStateOf(false)
            }
            var enablePtc = remember {
                mutableStateOf(false)
            }
            var ignoreMissingServices = remember {
                mutableStateOf(false)
            }

            if (!loaded.value) {
                settingsViewModel.initializeState(
                    isHostMapped,
                    useNce,
                    enableVsync, enableDocked, enablePtc, ignoreMissingServices
                )
                loaded.value = true
            }
            Scaffold(modifier = Modifier.fillMaxSize(),
                topBar = {
                    TopAppBar(title = {
                        Text(text = "Settings")
                    },
                        modifier = Modifier.padding(top = 16.dp),
                        navigationIcon = {
                            IconButton(onClick = {
                                settingsViewModel.save(
                                    isHostMapped,
                                    useNce,
                                    enableVsync,
                                    enableDocked,
                                    enablePtc,
                                    ignoreMissingServices
                                )
                                settingsViewModel.navController.popBackStack()
                            }) {
                                Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                            }
                        })
                }) { contentPadding ->
                Column(modifier = Modifier.padding(contentPadding)) {
                    BackHandler {
                        settingsViewModel.save(
                            isHostMapped,
                            useNce, enableVsync, enableDocked, enablePtc, ignoreMissingServices
                        )
                    }
                    ExpandableView(onCardArrowClick = { }, title = "System") {
                        Column(modifier = Modifier.fillMaxWidth()) {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Use NCE",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = useNce.value, onCheckedChange = {
                                    useNce.value = !useNce.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Is Host Mapped",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = isHostMapped.value, onCheckedChange = {
                                    isHostMapped.value = !isHostMapped.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Enable VSync",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = enableVsync.value, onCheckedChange = {
                                    enableVsync.value = !enableVsync.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Enable PTC",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = enablePtc.value, onCheckedChange = {
                                    enablePtc.value = !enablePtc.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Enable Docked Mode",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = enableDocked.value, onCheckedChange = {
                                    enableDocked.value = !enableDocked.value
                                })
                            }
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = "Ignore Missing Services",
                                    modifier = Modifier.align(Alignment.CenterVertically)
                                )
                                Switch(checked = ignoreMissingServices.value, onCheckedChange = {
                                    ignoreMissingServices.value = !ignoreMissingServices.value
                                })
                            }
                        }
                    }
                }
            }
        }

        @OptIn(ExperimentalMaterial3Api::class)
        @Composable
        @SuppressLint("UnusedTransitionTargetStateParameter")
        fun ExpandableView(
            onCardArrowClick: () -> Unit,
            title: String,
            content: @Composable () -> Unit
        ) {
            var expanded = false
            var mutableExpanded = remember {
                mutableStateOf(expanded)
            }
            val transitionState = remember {
                MutableTransitionState(expanded).apply {
                    targetState = !mutableExpanded.value
                }
            }
            val transition = updateTransition(transitionState, label = "transition")
            val cardPaddingHorizontal by transition.animateDp({
                tween(durationMillis = EXPANSTION_TRANSITION_DURATION)
            }, label = "paddingTransition") {
                if (mutableExpanded.value) 48.dp else 24.dp
            }
            val cardElevation by transition.animateDp({
                tween(durationMillis = EXPANSTION_TRANSITION_DURATION)
            }, label = "elevationTransition") {
                if (mutableExpanded.value) 24.dp else 4.dp
            }
            val cardRoundedCorners by transition.animateDp({
                tween(
                    durationMillis = EXPANSTION_TRANSITION_DURATION,
                    easing = FastOutSlowInEasing
                )
            }, label = "cornersTransition") {
                if (mutableExpanded.value) 0.dp else 16.dp
            }
            val arrowRotationDegree by transition.animateFloat({
                tween(durationMillis = EXPANSTION_TRANSITION_DURATION)
            }, label = "rotationDegreeTransition") {
                if (mutableExpanded.value) 0f else 180f
            }

            Card(
                shape = MaterialTheme.shapes.medium,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(
                        horizontal = cardPaddingHorizontal,
                        vertical = 8.dp
                    )
            ) {
                Column {
                    Card(
                        onClick = {
                            mutableExpanded.value = !mutableExpanded.value
                            onCardArrowClick()
                        }) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            CardTitle(title = title)
                            CardArrow(
                                degrees = arrowRotationDegree,
                            )

                        }
                    }
                    ExpandableContent(visible = mutableExpanded.value, content = content)
                }
            }
        }

        @Composable
        fun CardArrow(
            degrees: Float,
        ) {
            Icon(
                Icons.Filled.KeyboardArrowUp,
                contentDescription = "Expandable Arrow",
                modifier = Modifier.padding(8.dp).rotate(degrees),
            )
        }

        @Composable
        fun CardTitle(title: String) {
            Text(
                text = title,
                modifier = Modifier
                    .padding(16.dp),
                textAlign = TextAlign.Center,
            )
        }

        @Composable
        fun ExpandableContent(
            visible: Boolean = true,
            content: @Composable () -> Unit
        ) {
            val enterTransition = remember {
                expandVertically(
                    expandFrom = Alignment.Top,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                ) + fadeIn(
                    initialAlpha = 0.3f,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                )
            }
            val exitTransition = remember {
                shrinkVertically(
                    // Expand from the top.
                    shrinkTowards = Alignment.Top,
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                ) + fadeOut(
                    // Fade in with the initial alpha of 0.3f.
                    animationSpec = tween(EXPANSTION_TRANSITION_DURATION)
                )
            }

            AnimatedVisibility(
                visible = visible,
                enter = enterTransition,
                exit = exitTransition
            ) {
                Column(modifier = Modifier.padding(8.dp)) {
                    content()
                }
            }
        }
    }
}