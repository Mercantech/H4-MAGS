import 'dart:html' as html;
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'core/config/app_config.dart';
import 'core/di/injection.dart';
import 'features/weather/bloc/weather_bloc.dart';
import 'features/weather/view/weather_page.dart';
import 'features/infographic/view/infographic_page.dart';
import 'features/auth/view/auth_test_page.dart';
import 'features/auth/bloc/auth_bloc.dart';
import 'features/auth/bloc/auth_event.dart';
import 'features/auth/bloc/auth_state.dart';
import 'core/theme/theme.dart';

/// Main entry point
/// 
/// Initialiserer app dependencies og configuration før app starter.
/// 
/// Setup steps:
/// 1. Initialisér app configuration (environment)
/// 2. Setup dependency injection
/// 3. Start app
void main() async {
  // Sikr at Flutter bindings er initialiseret
  WidgetsFlutterBinding.ensureInitialized();

  // 1. Initialisér App Configuration
  // Detekter automatisk environment baseret på hostname
  // Hvis vi kører på localhost, brug development, ellers brug production
  final hostname = html.window.location.hostname ?? '';
  final isLocalhost = hostname == 'localhost' || 
                      hostname == '127.0.0.1' || 
                      hostname.isEmpty;
  final environment = isLocalhost ? Environment.development : Environment.production;
  
  await AppConfig.initialize(environment);
  
  // Log hvilket environment vi kører i
  debugPrint('🚀 Starting app in ${AppConfig.instance.environment.name} mode');
  debugPrint('🌐 Hostname: $hostname');
  debugPrint('📡 API Base URL: ${AppConfig.instance.apiBaseUrl}');

  // 2. Setup Dependency Injection
  await setupDependencyInjection();
  debugPrint('✅ Dependency Injection setup complete');

  // 3. Start App
  runApp(const MyApp());
}

/// Tip: Skift environment nemt
/// 
/// For at skifte mellem localhost og deployed API, ændre bare Environment i main():
/// - Development (localhost): Environment.development
/// - Production (deployed): Environment.production
/// - Staging (hvis I har det): Environment.staging

/// Root app widget
/// 
/// Setup BLoC providers og MaterialApp.
/// BLoCs injiceres via DI container (getIt).
class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        // Weather BLoC - injected via DI
        // Factory registration giver os ny instance hver gang
        BlocProvider(
          create: (context) => getIt<WeatherBloc>(),
        ),
        
        // Auth BLoC - injected via DI
        BlocProvider(
          create: (context) {
            final authBloc = getIt<AuthBloc>();
            // Tjek auth status ved opstart (læser fra secure storage)
            authBloc.add(const CheckAuthStatusEvent());
            return authBloc;
          },
        ),
      ],
      child: MaterialApp(
        title: 'H4 Vejr App',
        theme: appTheme,
        debugShowCheckedModeBanner: false,
        home: const MainNavigation(),
      ),
    );
  }
}

class MainNavigation extends StatefulWidget {
  const MainNavigation({super.key});

  @override
  State<MainNavigation> createState() => _MainNavigationState();
}

class _MainNavigationState extends State<MainNavigation> {
  int _selectedIndex = 0;

  static final List<Widget> _pages = <Widget>[
    WeatherPage(),
    InfographicPage(),
    const AuthTestPage(),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: _buildAppBar(context),
      body: _pages[_selectedIndex],
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: (index) {
          setState(() {
            _selectedIndex = index;
          });
        },
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.cloud),
            label: 'Vejr',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.info_outline),
            label: 'BLoC',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.login),
            label: 'Auth Test',
          ),
        ],
      ),
    );
  }

  PreferredSizeWidget _buildAppBar(BuildContext context) {
    return AppBar(
      backgroundColor: Theme.of(context).colorScheme.inversePrimary,
      title: BlocBuilder<AuthBloc, AuthState>(
        builder: (context, state) {
          // Centrer login state i midten
          return Center(
            child: _buildLoginState(context, state),
          );
        },
      ),
      centerTitle: false, // Vi centrerer manuelt
      automaticallyImplyLeading: false, // Fjern back button
    );
  }

  Widget _buildLoginState(BuildContext context, AuthState state) {
    if (state is AuthLoading) {
      return Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            width: 16,
            height: 16,
            child: CircularProgressIndicator(
              strokeWidth: 2,
              valueColor: AlwaysStoppedAnimation<Color>(
                Theme.of(context).colorScheme.onPrimary,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Text(
            'Tjekker login...',
            style: TextStyle(
              color: Theme.of(context).colorScheme.onPrimary,
              fontSize: 14,
            ),
          ),
        ],
      );
    }

    if (state is AuthAuthenticated) {
      final user = state.user;
      return Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Profile picture eller icon
          if (user.picture != null)
            ClipOval(
              child: Image.network(
                '${AppConfig.instance.apiBaseUrl.replaceAll('/api', '')}/api/users/${user.id}/picture',
                width: 24,
                height: 24,
                fit: BoxFit.cover,
                errorBuilder: (context, error, stackTrace) {
                  return Icon(
                    Icons.person,
                    size: 20,
                    color: Theme.of(context).colorScheme.onPrimary,
                  );
                },
              ),
            )
          else
            Icon(
              Icons.person,
              size: 20,
              color: Theme.of(context).colorScheme.onPrimary,
            ),
          const SizedBox(width: 8),
          // Username eller email
          Text(
            user.username.isNotEmpty ? user.username : user.email,
            style: TextStyle(
              color: Theme.of(context).colorScheme.onPrimary,
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(width: 4),
          Icon(
            Icons.check_circle,
            size: 16,
            color: Colors.green[300],
          ),
        ],
      );
    }

    // Not authenticated
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(
          Icons.person_outline,
          size: 20,
          color: Theme.of(context).colorScheme.onPrimary,
        ),
        const SizedBox(width: 8),
        Text(
          'Ikke logget ind',
          style: TextStyle(
            color: Theme.of(context).colorScheme.onPrimary,
            fontSize: 14,
          ),
        ),
      ],
    );
  }
}


