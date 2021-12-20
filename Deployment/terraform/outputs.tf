output "manifest_path" {
  value = abspath(data.archive_file.dotfiles.output_path)
}
