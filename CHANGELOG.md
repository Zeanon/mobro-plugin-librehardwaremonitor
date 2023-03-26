# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 0.1.1 - Unreleased

### Changed

- Updated to LibreHardwareMonitor 0.9.2

## 0.1.0 - 2023-01-23

### Added

- Support for metrics in battery category
- Support for electric current based metrics
- Support for duration based metric values

### Changed

- Parse metrics of no specific type as numeric instead of text where applicable
- Sensor as struct to reduce memory allocations
- Updated to new SDK
- Updated to .NET 7

## 0.0.2 - 2022-11-06

### Fixed

- Throughput being returned in bytes instead of bits

## 0.0.1 - 2022-10-17

### Added

- Initial release
