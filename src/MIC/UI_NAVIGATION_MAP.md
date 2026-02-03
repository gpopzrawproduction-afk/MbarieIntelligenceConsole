# UI Navigation Map - Mbarie Intelligence Console (MIC)

## Overview
This document defines the complete user interface navigation flow for the Mbarie Intelligence Console application, including all screens, transitions, and user interactions.

## 1. Application Entry Points

### 1.1 Startup Flow
```
App Launch → SplashScreen (Optional) → LoginWindow
```

### 1.2 Authentication States
```
┌─────────────────────────────────────────────────────────────┐
│                       Application Start                      │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                         LoginWindow                          │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ Language Selector: English / Français / Español /   │    │
│  │                   العربية / 中文                    │    │
│  └─────────────────────────────────────────────────────┘    │
│  Username: [_______________]                                 │
│  Password: [_______________]                                 │
│  ☐ Remember Me                                               │
│  ┌─────────────────┐   ┌─────────────────┐                  │
│  │     Login       │   │    Register     │                  │
│  └─────────────────┘   └─────────────────┘                  │
│  (Guest login option REMOVED per requirements)               │
└─────────────────────────────┬───────────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │                   │
           Valid Credentials    Invalid Credentials
                    │                   │
                    ▼                   ▼
           ┌─────────────────┐   ┌─────────────────┐
           │ Create Session  │   │ Show Error      │
           │ Set Language    │   │ (localized)     │
           └─────────────────┘   └─────────────────┘
                    │
                    ▼
           ┌─────────────────┐
           │   MainWindow    │
           │   (Dashboard)   │
           └─────────────────┘
```

## 2. Main Application Navigation

### 2.1 MainWindow Layout
```
┌─────────────────────────────────────────────────────────────────────────┐
│                             MainWindow                                   │
├─────────────────────────────────────────────────────────────────────────┤
│  Header Bar                                                             │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [MIC Logo] Mbarie Intelligence Console       [User Profile]     │   │
│  │ Connection: ● Connected | Last Update: 14:30 [Notifications]    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────┤
│  Navigation Sidebar           │ Content Area                            │
│  ┌─────────────────────────┐  │                                         │
│  │ ● Dashboard             │  │                                         │
│  │ ○ Alerts                │  │ ┌─────────────────────────────────┐    │
│  │ ○ Metrics               │  │ │ Current View                    │    │
│  │ ○ Predictions           │  │ │ (Dashboard/Alerts/Metrics/etc)  │    │
│  │ ○ AI Chat               │  │ └─────────────────────────────────┘    │
│  │ ○ Email                 │  │                                         │
│  │ ○ Settings              │  │                                         │
│  │                         │  │                                         │
│  │ [Command Palette] Ctrl+K│  │                                         │
│  └─────────────────────────┘  │                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Navigation State Transitions
```
Current View → Action → Next View
─────────────────────────────────────────────────────────────────────────
Dashboard    → Click "Alerts"       → AlertListView
Dashboard    → Click "Check Inbox"  → EmailInboxView (or AddEmailAccountWindow)
Dashboard    → Click "AI Chat"      → ChatView
Any View     → Click "Dashboard"    → DashboardView
Any View     → Click "Settings"     → SettingsView
Any View     → Ctrl+K               → Command Palette Overlay
```

## 3. View-Specific Navigation

### 3.1 DashboardView
```
┌─────────────────────────────────────────────────────────────────────────┐
│                             DashboardView                                │
├─────────────────────────────────────────────────────────────────────────┤
│  KPI Cards (4)                                                          │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐                       │
│  │ Alerts  │ │ Metrics │ │ Email   │ │ AI      │                       │
│  │  12 ▲   │ │ 85% ✓   │ │ 24 ✉    │ │ Ready   │                       │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘                       │
│                                                                         │
│  Quick Actions                                                          │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐           │
│  │   Refresh Data  │ │   Check Inbox   │ │ View Urgent     │           │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘           │
│                                                                         │
│  Recent Alerts                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ • High Temperature Detected (Critical)           [View All]     │   │
│  │ • Maintenance Due (Warning)                      [Acknowledge]  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Metrics Chart                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [Line Chart: Production Output Trend]                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘

Navigation from Dashboard:
• Click "View All" in Recent Alerts → AlertListView
• Click "Check Inbox" → EmailInboxView (or setup flow)
• Click "View Urgent" → AlertListView (filtered: Severity >= Warning)
• Click AI KPI Card → ChatView
```

### 3.2 AlertListView
```
┌─────────────────────────────────────────────────────────────────────────┐
│                             AlertListView                                │
├─────────────────────────────────────────────────────────────────────────┤
│  Toolbar                                                                 │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐           │
│  │   New Alert     │ │    Refresh      │ │    Filters      │           │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘           │
│                                                                         │
│  Filter Controls                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Severity: [All ▼] Status: [All ▼] Date: [Last 7 days ▼]        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Alerts Table                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Name             │ Severity │ Status   │ Source    │ Actions   │   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │ High Temperature │ Critical │ Active   │ Monitoring│ [Edit]    │   │
│  │ Maintenance Due  │ Warning  │ Active   │ Asset Mgt │ [Acknow]  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘

Navigation from AlertListView:
• Click "New Alert" → CreateAlertDialog (Modal)
• Click "Edit" on alert → AlertDetailsView (or inline edit)
• Click "Acknowledge" → Update alert status (stays in list)
• Click back to navigation → Previous view
```

### 3.3 EmailInboxView
```
┌─────────────────────────────────────────────────────────────────────────┐
│                             EmailInboxView                               │
├─────────────────────────────────────────────────────────────────────────┤
│  Account Selection & Actions                                            │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Account: [alex@company.com ▼]      [Sync] [Add Account]        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Folder Navigation                                                      │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │
│  │ Inbox   │ │ Sent    │ │ Drafts  │ │ Archive │ │ Trash   │          │
│  │ (24)    │ │ (12)    │ │ (3)     │ │ (45)    │ │ (8)     │          │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘          │
│                                                                         │
│  Email List                                                             │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Sender         │ Subject        │ Date     │ Priority │ Actions│   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │ CEO            │ Company Update │ 10:30    │ ● High   │ [Read] │   │
│  │ Project Mgr    │ Status Report  │ 09:15    │ ○ Normal │ [Flag] │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Email Preview/Detail (Right Panel - Optional Split View)               │
└─────────────────────────────────────────────────────────────────────────┘

Navigation from EmailInboxView:
• Click "Add Account" → AddEmailAccountWindow (Modal)
• Click email row → EmailDetailView (or preview pane)
• Click folder tab → Filter emails by folder
• No accounts configured → Auto-navigate to AddEmailAccountWindow
```

### 3.4 ChatView
```
┌─────────────────────────────────────────────────────────────────────────┐
│                             ChatView                                     │
├─────────────────────────────────────────────────────────────────────────┤
│  Conversation Header                                                    │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ AI Assistant                    [Clear] [Export]               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Message History                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ User: How can I improve production efficiency?                  │   │
│  │ AI: Based on your metrics, I recommend... (14:30)              │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Input Area                                                            │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [Type your message...] (Shift+Enter for new line)              │   │
│  │ ┌─────────────────┐                                             │   │
│  │ │      Send       │                                             │   │
│  │ └─────────────────┘                                             │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Suggested Questions                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ • Analyze recent email trends                                   │   │
│  │ • Generate alert summary report                                 │   │
│  │ • Predict next quarter metrics                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘

Navigation from ChatView:
• Click "Export" → File save dialog
• Click "Clear" → Confirmation dialog → Clear conversation
• Enter text + Enter → Send message (stays in chat)
• Click suggested question → Auto-populate input
```

### 3.5 SettingsView
```
┌─────────────────────────────────────────────────────────────────────────┐
│                             SettingsView                                 │
├─────────────────────────────────────────────────────────────────────────┤
│  Settings Categories (Tab Navigation)                                   │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐                       │
│  │ General │ │ AI      │ │ Email   │ │ Account │                       │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘                       │
│                                                                         │
│  General Settings                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Language: [English ▼]                                          │   │
│  │   • English / Français / Español / العربية / 中文              │   │
│  │                                                                 │   │
│  │ Theme: [Dark ▼]                                                 │   │
│  │   • Dark / Light / System                                       │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  AI Settings                                                            │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Provider: [OpenAI ▼]                                            │   │
│  │ API Key: [●●●●●●●●●●●●●●] [Test Connection]                     │   │
│  │ Model: [gpt-4-turbo ▼]                                          │   │
│  │ Temperature: [0.7 ──────●─────────]                             │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Action Bar                                                            │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐           │
│  │     Save        │ │     Cancel      │ │  Reset Defaults │           │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘           │
└─────────────────────────────────────────────────────────────────────────┘

Navigation from SettingsView:
• Change language → Immediate UI refresh (no navigation)
• Click "Test Connection" → API test (stays in settings)
• Click "Save" → Persist settings (stays in settings)
• Click tab headers → Switch settings category
```

## 4. Modal Windows & Dialogs

### 4.1 AddEmailAccountWindow (Modal)
```
┌─────────────────────────────────────────────────────────────────────────┐
│                     AddEmailAccountWindow (Modal)                        │
├─────────────────────────────────────────────────────────────────────────┤
│  Header                                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Add Email Account                           [×]                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Provider Selection                                                     │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Select Email Provider:                                          │   │
│  │   (○) Gmail                                                     │   │
│  │   (○) Outlook/Office 365                                        │   │
│  │   (○) Other (IMAP)                                              │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  OAuth Flow (Gmail/Outlook)                                            │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [Connect with Google] or [Connect with Microsoft]              │   │
│  │                                                                 │   │
│  │ After clicking, browser opens for OAuth authorization          │   │
│  │ Returns to app with access token                               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Manual Configuration (IMAP)                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Email: [__________________________]                             │   │
│  │ IMAP Server: [___________________] Port: [993]                 │   │
│  │ Username: [______________________]                              │   │
│  │ Password: [●●●●●●●●●●●●●●●●●●●●●]                               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Action Buttons                                                        │
│  ┌─────────────────┐ ┌─────────────────┐                               │
│  │     Save        │ │     Cancel      │                               │
│  └─────────────────┘ └─────────────────┘                               │
└─────────────────────────────────────────────────────────────────────────┘

Flow: EmailInboxView → AddEmailAccountWindow → OAuth flow → Return to EmailInboxView
```

### 4.2 CreateAlertDialog (Modal)
```
┌─────────────────────────────────────────────────────────────────────────┐
│                       CreateAlertDialog (Modal)                          │
├─────────────────────────────────────────────────────────────────────────┤
│  Header                                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Create New Alert                            [×]                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Form Fields                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Alert Name: [________________________________]                  │   │
│  │ Description: [_______________________________]                  │   │
│  │            [multiline text area]                               │   │
│  │ Severity: [Critical ▼]                                          │   │
│  │   • Critical / Warning / Info                                   │   │
│  │ Source: [___________________________________]                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Action Buttons                                                        │
│  ┌─────────────────┐ ┌─────────────────┐                               │
│  │     Create      │ │     Cancel      │                               │
│  └─────────────────┘ └─────────────────┘                               │
└─────────────────────────────────────────────────────────────────────────┘

Flow: AlertListView → CreateAlertDialog → Save → Refresh AlertListView
```

### 4.3 Command Palette (Overlay)
```
┌─────────────────────────────────────────────────────────────────────────┐
│                       Command Palette (Ctrl+K)                          │
├─────────────────────────────────────────────────────────────────────────┤
│  Search Input                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Type a command...                                               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Command Results                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Navigate to Dashboard                                           │   │
│  │ Navigate to Alerts                                              │   │
│  │ Navigate to Email                                               │   │
│  │ New Alert                                                       │   │
│  │ Sync Email                                                       │   │
│  │ Open Settings                                                   │   │
│  │ Logout                                                          │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘

Navigation: Anywhere in app → Ctrl+K → Type command → Execute → Close palette
```

## 5. Error States & Empty States

### 5.1 Empty States
```
• No Alerts: "No alerts found. Create your first alert."
• No Email Accounts: "No email accounts configured. Add an account to get started."
• No Emails: "No emails in this folder. Sync your account to load emails."
• No Chat History: "Start a conversation with the AI assistant."
• No Metrics Data: "No metrics data available. Check your data sources."
```

### 5.2 Error States
```
• Login Error: "Invalid credentials. Please try again." (localized)
• Network Error: "Cannot connect to server. Check your connection."
• Sync Error: "Email sync failed. Please try again."
• API Error: "AI service unavailable. Check your API key in settings."
• Permission Error: "Access denied. Please contact your administrator."
```

### 5.3 Loading States
```
• Dashboard: "Loading dashboard data..."
• Email List: "Loading emails..."
• AI Response: "Thinking..." with spinner
• Sync: "Syncing emails..." with progress indicator
```

## 6. Keyboard Navigation Map

### 6.1 Global Shortcuts
```
Ctrl+K      → Open Command Palette
Ctrl+Q      → Logout (with confirmation)
Ctrl+R      → Refresh current view
Ctrl+S      → Save (in forms/dialogs)
Esc         → Close modal/cancel
Tab         → Navigate between form fields
Shift+Tab   ← Navigate backward between form fields
Enter       → Submit forms, send chat messages
Shift+Enter → New line in multi-line inputs
```

### 6.2 View-Specific Shortcuts
```
Dashboard:
  Ctrl+1 → Focus Alerts section
  Ctrl+2 → Focus Metrics section
  Ctrl+3 → Focus Email section
  
AlertListView:
  Ctrl+N → New Alert
  Ctrl+F → Focus filter
  Enter  → Edit selected alert
  
EmailInboxView:
  Ctrl+N → New email (future)
  Ctrl+R → Reply (future)
  Ctrl+S → Sync
  
ChatView:
  Ctrl+L → Clear conversation
  Ctrl+E → Export conversation
  
SettingsView:
  Ctrl+T → Test AI connection
  Ctrl+L → Focus language selector
```

## 7. Mobile/Responsive Considerations

### 7.1 Breakpoints
```
• Desktop (≥1200px): Full sidebar navigation
• Tablet (768px-1199px): Collapsed sidebar, hamburger menu
• Mobile (<768px): Bottom navigation bar, single-column layout
```

### 7.2 Adaptive Layout
```
Desktop:
  ┌───┬─────┐
  │Nav│Content
  └───┴─────┘

Tablet:
  ┌─────────┐
  │[☰] Header
  ├─────────┤
  │ Content │
  └─────────┘
  (Nav slides in from left)

Mobile:
  ┌─────────┐
  │ Header  │
  ├─────────┤
  │ Content │
  ├─────────┤
  │ Nav Bar │
  └─────────┘
  (Bottom navigation: icons only)
```

## 8. Localization Navigation Impact

### 8.1 Language-Specific Considerations
```
• Arabic (العربية): Right-to-Left layout
  - Navigation sidebar moves to right
  - Text alignment right
  - Icon positions mirrored
  
• Chinese (中文): Character-based layout
  - Text may be shorter/longer
  - Adjust column widths
  
• All Languages:
  - Date/time formats localized
  - Number formats localized
  - Directional icons may flip for RTL
```

### 8.2 Language Switching Flow
```
Current View → Open Settings → Change Language → Confirm → UI Reloads
                                                                   │
               ┌───────────────────────────────────────────────────┘
               ▼
      All text resources reloaded
      Layout adjusts for RTL if needed
      User preference saved to DB
      Returns to same view with new language
```

## 9. User State Transitions

### 9.1 Session Management
```
Logged In → Logout → LoginWindow (with username pre-filled if "Remember Me")
Logged In → Inactivity (30min) → Lock screen → Password required
Logged In → App minimized → Background sync continues
Logged In → Network lost → Offline mode → Queue actions
```

### 9.2 Data State Transitions
```
Email Sync:
  Idle → Syncing → Success/Failed → Idle
  
AI Chat:
  Idle → Processing → Response → Idle
  
Alert Creation:
  Form empty → Form valid → Submitting → Success/Error
  
Settings Change:
  Original → Modified → Saving → Applied/Reverted
```

## 10. Navigation Validation Rules

### 10.1 Access Control
```
• Guest access: DISABLED (removed per requirements)
• Unauthenticated users: Only LoginWindow accessible
• Authenticated users: Full navigation access
• Admin users: Additional admin-only views (future)
```

### 10.2 Feature Availability
```
• Email navigation: Only if at least one email account configured
• AI Chat: Only if API key configured in settings
• Real-time metrics: Only if data sources connected
• Predictions: Only if historical data available
```

### 10.3 State Preservation
```
• Navigation history: Last 10 views remembered
• Form data: Preserved during navigation (if unsaved, prompt)
• Filter states: Remembered per view
• Scroll positions: Restored when returning to view
```

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-30  
**Author:** Cline (AI Assistant)  
**Status:** Complete