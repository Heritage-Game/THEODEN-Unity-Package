using System;
using System.Collections.Generic;

[Serializable]
public class LocalizationModel
{
    public List<LocalizationEntry> languages;
}

[Serializable]
public class LocalizationEntry
{
    public string language;
    public UILocalization uiTexts;
}

[Serializable]
public class UILocalization
{
    public string back_button;
    public string continue_button;
    public string play_button;
    public string hint_button;
    public string submit_button;
    public string scan_qr_button;
    public string language_title;
    public string language_subtitle;
    public string nickname_title;
    public string nickname_subtitle;
    public string nickname_placeholder;
    public string nickname_error;
    public string hello_label;
    public string menu_title;
    public string discover_button;
    public string show_map_button;
    public string codex_button;
    public string leaderboard_button;
    public string menu_language_label;
    public string menu_instructions_label;
    public string instructions_title;
    public string instructions_text;
    public string challenge_title;
    public string badge_unlocked;
    public string badge_label;
    public string points_label;
    public string attempts_label;
    public string correct_label;
    public string challenge_completed_label;
    public string wrong_label;
    public string no_attempts_label;
    public string try_again_label;
    public string attempts_progress;
    public string correct_answer;
    public string wrong_answer;
    public string hint_label;
    public string no_hint_label;
    public string welcome_text;
    public string points_earned;
    public string status_scanning;
    public string status_requesting;
    public string status_denied;
    public string status_not_found;
    public string status_cannot_load;
    public string status_datamanager_missing;
    public string status_no_selected_level;
    public string status_no_valid_id;
    public string status_wrong_qr_code;
    public string status_no_session;
    public string status_correct;
    public string status_no_poi;
    public string status_no_navigation_manager;
}