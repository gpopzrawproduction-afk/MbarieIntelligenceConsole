# Current State Analysis - Alert Dialogs & Scrolling

## Scrolling Fixes Applied:
✅ **LoginWindow.axaml** - Added ScrollViewer wrapper around content
✅ **AddEmailAccountWindow.axaml** - Added ScrollViewer wrapper around content

## Alert Infrastructure Analysis:

### Existing Files:
✅ **CreateAlertDialog.axaml** - Exists with proper XAML structure
✅ **CreateAlertDialog.axaml.cs** - Exists, creates CreateAlertViewModel
✅ **CreateAlertViewModel.cs** - Exists, handles CreateAlertCommand via MediatR
✅ **AlertListViewModel.cs** - Has CreateAlertRequested and EditAlertRequested events
✅ **MainWindow.xaml.cs** - References CreateAlertDialog in ShowCreateAlertDialogAsync method

### Missing Files:
❌ **EditAlertDialog.axaml** - Does not exist
❌ **EditAlertDialog.axaml.cs** - Does not exist
❌ **EditAlertViewModel.cs** - Does not exist

### Event Wiring Status:
- AlertListViewModel has events defined but they're not being subscribed to anywhere
- MainWindow has ShowCreateAlertDialogAsync but not connected to AlertListViewModel events
- AlertListView.axaml has "NEW ALERT" button bound to CreateAlertCommand, which triggers CreateAlertRequested event

## Required Actions:

### 1. Create Missing Edit Alert Dialog
- Create EditAlertDialog.axaml (similar to CreateAlertDialog but with pre-population)
- Create EditAlertDialog.axaml.cs 
- Create EditAlertViewModel.cs (or reuse CreateAlertViewModel with modifications)

### 2. Wire Events in AlertListView
- Subscribe to CreateAlertRequested and EditAlertRequested events in AlertListView.xaml.cs
- Show appropriate dialogs when events are raised

### 3. Fix Event Flow
- Ensure CreateAlertCommand in AlertListViewModel properly triggers CreateAlertRequested
- Ensure EditAlertCommand in AlertListViewModel properly triggers EditAlertRequested
- Handle dialog results and refresh alert list

### 4. Check Other Screens for Scrolling Issues
- Check DashboardView, SettingsView, etc. for ScrollViewer presence