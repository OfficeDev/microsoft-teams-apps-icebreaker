variable "stage" {
  type        = string
  description = "Three characters long stage eg. dev, prd. Will be used for names"
}

variable "name" {
  type        = string
  description = "Name of the App"
  default     = "Icebreaker"
}

variable "suffix" {
  type        = string
  description = "Two character suffix to avoid name clashes"
  default     = "01"
}

variable "pairingWeekInterval" {
  type        = number
  default     = 1
  description = "The number of weeks between pairings."
}

variable "pairingDayOfWeek" {
  type        = string
  default     = "Monday"
  description = "The day of the week when pairings are created."
}

variable "pairingHour" {
  type        = number
  default     = 10
  description = "The hour at which pairings are created."
}

variable "pairingTimeZone" {
  type        = string
  default     = "W. Europe Standard Time"
  description = "The time zone for the hour at which pairings are created."
}

variable "sku" {
  type        = string
  default     = "Free"
  description = "The pricing tier for the hosting plan."
}

variable "defaultCulture" {
  type        = string
  default     = "de"
  description = "Default culture. You can select default culture from the following: en, de"
}

variable "gitRepoUrl" {
  type        = string
  default     = "https://github.com/WhiteTomX/microsoft-teams-apps-icebreaker.git"
  description = "The URL to the GitHub repository to deploy."
}

variable "appDisplayName" {
  type        = string
  default     = "Icebreaker"
  description = "The app (and bot) display name."
}

variable "description_short" {
  type        = string
  default     = "Bringt Zeichensetzende nach dem Zufallsprinzip zusammen."
  description = "The short description must not be longer than 80 characters"
}

variable "description_long" {
  type        = string
  default     = "Icebreaker ist ein niedlicher kleiner Bot, der jede Woche nach dem Zufallsprinzip Zeichensetzende zusammenbringt, um zu helfen, Vertrauen und persönliche Beziehungen aufzubauen."
  description = "The app (and bot) description."
}

variable "companyName" {
  type        = string
  default     = "Zeichen gegen Mobbing"
  description = "The display name for the company."
}

variable "websiteUrl" {
  type        = string
  default     = "https://zeichen-gegen-mobbing.de"
  description = "The https:// URL to the company's website. This link should take users to your company or product-specific landing page."
}

variable "privacyUrl" {
  type        = string
  default     = "https://zeichen-gegen-mobbing.de/datenschutz"
  description = "The https:// URL to the company's privacy policy."
}

variable "termsOfUseUrl" {
  type        = string
  default     = "https://zeichen-gegen-mobbing.de/impressum"
  description = "The https:// URL to the company's terms of use."
}
