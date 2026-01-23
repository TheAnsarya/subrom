Name:           subrom
Version:        1.2.0
Release:        1%{?dist}
Summary:        ROM management and verification toolkit

License:        MIT
URL:            https://github.com/TheAnsarya/subrom
Source0:        %{name}-%{version}.tar.gz

BuildRequires:  dotnet-sdk-10.0
Requires:       libicu

%description
Subrom is a ROM management and verification toolkit that helps
you organize, verify, and manage your ROM collection against
No-Intro, TOSEC, and other DAT file catalogs.

Features:
- DAT file parsing (XML, ClrMamePro formats)
- Multi-format archive support (ZIP, 7z, RAR)
- Real-time scanning with progress
- Web-based user interface
- Background service support

%prep
%setup -q

%build
dotnet publish src/Subrom.Server/Subrom.Server.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:Version=%{version} \
    -o %{_builddir}/publish

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}/opt/subrom
mkdir -p %{buildroot}/lib/systemd/system
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps
mkdir -p %{buildroot}/var/lib/subrom
mkdir -p %{buildroot}/var/log/subrom

cp -r %{_builddir}/publish/* %{buildroot}/opt/subrom/
install -m 644 installers/linux/debian/subrom.service %{buildroot}/lib/systemd/system/
install -m 644 installers/linux/debian/subrom.desktop %{buildroot}/usr/share/applications/

%pre
# Create user if not exists
getent passwd subrom >/dev/null || useradd --system --no-create-home --shell /sbin/nologin subrom

%post
systemctl daemon-reload
systemctl enable subrom.service
systemctl start subrom.service

%preun
if [ $1 -eq 0 ]; then
    systemctl stop subrom.service
    systemctl disable subrom.service
fi

%postun
systemctl daemon-reload
if [ $1 -eq 0 ]; then
    userdel subrom 2>/dev/null || true
fi

%files
%license LICENSE
%doc README.md
/opt/subrom/
/lib/systemd/system/subrom.service
/usr/share/applications/subrom.desktop
%dir %attr(755,subrom,subrom) /var/lib/subrom
%dir %attr(755,subrom,subrom) /var/log/subrom

%changelog
* Wed Jan 22 2026 Subrom Project <support@subrom.app> - 1.2.0-1
- Initial RPM release
- Cross-platform installer support
