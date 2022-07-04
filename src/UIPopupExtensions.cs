using System.Linq;

public static class UIPopupExtensions
{
    public const int maxVisibleCount = 400;

    public static void SelectPrevious(this UIPopup uiPopup)
    {
        if (uiPopup.currentValue == uiPopup.popupValues.First())
        {
            uiPopup.currentValue = uiPopup.LastVisibleValue();
        }
        else
        {
            uiPopup.SetPreviousValue();
        }
    }

    public static void SelectNext(this UIPopup uiPopup)
    {
        if (uiPopup.currentValue == uiPopup.LastVisibleValue())
        {
            uiPopup.currentValue = uiPopup.popupValues.First();
        }
        else
        {
            uiPopup.SetNextValue();
        }
    }

    public static string LastVisibleValue(this UIPopup uiPopup) =>
        uiPopup.popupValues.Length > maxVisibleCount
            ? uiPopup.popupValues[maxVisibleCount - 1]
            : uiPopup.popupValues.Last();
}
