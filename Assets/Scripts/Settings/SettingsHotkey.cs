using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Esc tuşunun tek sahibi: koleksiyon defteri açıksa onu kapatır, değilse ayarlar ekranını açar/kapatır.
/// Esc'yi tek yerde dinlemek, iki ekranın aynı basışta birden tepki vermesini önler. Kalıcı servis nesnesinde durur.
/// </summary>
public class SettingsHotkey : MonoBehaviour
{
    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (CollectionBookScreen.IsOpen)
        {
            GameAudio.Click();
            CollectionBookScreen.Close();
            return;
        }

        GameAudio.Click();
        SettingsScreen.Toggle();
    }
}
