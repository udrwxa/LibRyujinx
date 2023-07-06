package org.ryujinx.android.viewmodels

import android.content.SharedPreferences
import androidx.compose.runtime.MutableState
import androidx.navigation.NavHostController
import androidx.preference.PreferenceManager
import org.ryujinx.android.MainActivity

class SettingsViewModel(var navController: NavHostController, val activity: MainActivity) {
    private var sharedPref: SharedPreferences

    init {
        sharedPref = getPreferences()
    }

    private fun getPreferences() : SharedPreferences {
        return PreferenceManager.getDefaultSharedPreferences(activity)
    }

    fun initializeState(isHostMapped : MutableState<Boolean>,
                        useNce : MutableState<Boolean>,
                        enableVsync : MutableState<Boolean>,
                        enableDocked : MutableState<Boolean>,
                        enablePtc : MutableState<Boolean>,
                        ignoreMissingServices : MutableState<Boolean>)
    {

        isHostMapped.value = sharedPref.getBoolean("isHostMapped", true)
        useNce.value = sharedPref.getBoolean("useNce", true)
        enableVsync.value = sharedPref.getBoolean("enableVsync", true)
        enableDocked.value = sharedPref.getBoolean("enableDocked", true)
        enablePtc.value = sharedPref.getBoolean("enablePtc", true)
        ignoreMissingServices.value = sharedPref.getBoolean("ignoreMissingServices", false)
    }

    fun save(isHostMapped : MutableState<Boolean>,
             useNce : MutableState<Boolean>,
             enableVsync : MutableState<Boolean>,
             enableDocked : MutableState<Boolean>,
             enablePtc : MutableState<Boolean>,
             ignoreMissingServices : MutableState<Boolean>){
        var editor = sharedPref.edit()

        editor.putBoolean("isHostMapped", isHostMapped?.value ?: true)
        editor.putBoolean("useNce", useNce?.value ?: true)
        editor.putBoolean("enableVsync", enableVsync?.value ?: true)
        editor.putBoolean("enableDocked", enableDocked?.value ?: true)
        editor.putBoolean("enablePtc", enablePtc?.value ?: true)
        editor.putBoolean("ignoreMissingServices", ignoreMissingServices?.value ?: false)

        editor.apply()
    }
}