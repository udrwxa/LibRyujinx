package org.ryujinx.android

import android.view.KeyEvent
import android.view.MotionEvent

class PhysicalControllerManager(val activity: MainActivity) {
    private var controllerId: Int = -1
    private var ryujinxNative: RyujinxNative = RyujinxNative.instance

    fun onKeyEvent(event: KeyEvent) : Boolean{
        val id = getGamePadButtonInputId(event.keyCode)
        if(id != GamePadButtonInputId.None) {
            val isNotFallback = (event.flags and KeyEvent.FLAG_FALLBACK) == 0
            if (controllerId != -1 &&  isNotFallback) {
                when (event.action) {
                    KeyEvent.ACTION_UP -> {
                        ryujinxNative.inputSetButtonReleased(id.ordinal, controllerId)
                    }

                    KeyEvent.ACTION_DOWN -> {
                        ryujinxNative.inputSetButtonPressed(id.ordinal, controllerId)
                    }
                }
                return true
            }
            else if(!isNotFallback){
                return true
            }
        }

        return  false
    }

    fun onMotionEvent(ev: MotionEvent) {
        if(controllerId != -1) {
            if(ev.action == MotionEvent.ACTION_MOVE) {
                val leftStickX = ev.getAxisValue(MotionEvent.AXIS_X)
                val leftStickY = ev.getAxisValue(MotionEvent.AXIS_Y)
                val rightStickX = ev.getAxisValue(MotionEvent.AXIS_Z)
                val rightStickY = ev.getAxisValue(MotionEvent.AXIS_RZ)
                ryujinxNative.inputSetStickAxis(1, leftStickX, -leftStickY ,controllerId)
                ryujinxNative.inputSetStickAxis(2, rightStickX, -rightStickY ,controllerId)
            }
        }
    }

    fun connect() : Int {
        controllerId = ryujinxNative.inputConnectGamepad(0)
        return controllerId
    }

    fun disconnect(){
        controllerId = -1
    }

    private fun getGamePadButtonInputId(keycode: Int): GamePadButtonInputId {
        return when (keycode) {
            KeyEvent.KEYCODE_BUTTON_A -> GamePadButtonInputId.B
            KeyEvent.KEYCODE_BUTTON_B -> GamePadButtonInputId.A
            KeyEvent.KEYCODE_BUTTON_X -> GamePadButtonInputId.X
            KeyEvent.KEYCODE_BUTTON_Y -> GamePadButtonInputId.Y
            KeyEvent.KEYCODE_BUTTON_L1 -> GamePadButtonInputId.LeftShoulder
            KeyEvent.KEYCODE_BUTTON_L2 -> GamePadButtonInputId.LeftTrigger
            KeyEvent.KEYCODE_BUTTON_R1 -> GamePadButtonInputId.RightShoulder
            KeyEvent.KEYCODE_BUTTON_R2 -> GamePadButtonInputId.RightTrigger
            KeyEvent.KEYCODE_BUTTON_THUMBL -> GamePadButtonInputId.LeftStick
            KeyEvent.KEYCODE_BUTTON_THUMBR -> GamePadButtonInputId.RightStick
            KeyEvent.KEYCODE_DPAD_UP -> GamePadButtonInputId.DpadUp
            KeyEvent.KEYCODE_DPAD_DOWN -> GamePadButtonInputId.DpadDown
            KeyEvent.KEYCODE_DPAD_LEFT -> GamePadButtonInputId.DpadLeft
            KeyEvent.KEYCODE_DPAD_RIGHT -> GamePadButtonInputId.DpadRight
            KeyEvent.KEYCODE_BUTTON_START -> GamePadButtonInputId.Plus
            KeyEvent.KEYCODE_BUTTON_SELECT -> GamePadButtonInputId.Minus
            else -> GamePadButtonInputId.None
        }
    }
}
