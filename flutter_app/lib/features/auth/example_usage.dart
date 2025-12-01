/// Eksempel på hvordan man bruger Google Sign-In i Flutter appen
/// 
/// Dette er et eksempel - du skal tilpasse det til din egen app struktur.

/*
import 'package:flutter/material.dart';
import '../../data/repositories/auth_repository_impl.dart';
import '../../data/datasources/auth_remote_datasource.dart';
import '../../core/api/api_client.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _authRepository = AuthRepositoryImpl(
    remoteDataSource: AuthRemoteDataSource(
      apiClient: ApiClient(),
    ),
  );

  bool _isLoading = false;

  Future<void> _loginWithGoogle() async {
    setState(() => _isLoading = true);

    final result = await _authRepository.loginWithGoogle();

    result.when(
      success: (authResponse) async {
        // Gem token (brug din egen storage løsning)
        // await storageService.saveToken(authResponse.token);
        // await storageService.saveRefreshToken(authResponse.refreshToken);
        
        // Naviger til hovedside
        if (mounted) {
          Navigator.of(context).pushReplacementNamed('/home');
        }
      },
      failure: (error) {
        // Vis fejlbesked
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Login fejlede: ${error.userMessage}'),
              backgroundColor: Colors.red,
            ),
          );
        }
      },
    );

    if (mounted) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Login')),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            ElevatedButton.icon(
              onPressed: _isLoading ? null : _loginWithGoogle,
              icon: const Icon(Icons.login),
              label: const Text('Login med Google'),
            ),
            if (_isLoading) const CircularProgressIndicator(),
          ],
        ),
      ),
    );
  }
}
*/

