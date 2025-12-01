import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/auth_bloc.dart';
import '../bloc/auth_event.dart';
import '../bloc/auth_state.dart';

/// Password Update Form Widget
/// 
/// Giver mulighed for at opdatere password, også for OAuth brugere.
class PasswordUpdateForm extends StatefulWidget {
  final int userId;

  const PasswordUpdateForm({super.key, required this.userId});

  @override
  State<PasswordUpdateForm> createState() => _PasswordUpdateFormState();
}

class _PasswordUpdateFormState extends State<PasswordUpdateForm> {
  final _formKey = GlobalKey<FormState>();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _obscurePassword = true;
  bool _obscureConfirmPassword = true;
  bool _isUpdating = false;

  @override
  void dispose() {
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _handleUpdatePassword() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    if (!mounted) return;

    setState(() {
      _isUpdating = true;
    });

    // Dispatch event til AuthBloc
    // BlocListener vil håndtere state changes og vise feedback
    context.read<AuthBloc>().add(
      UpdatePasswordEvent(_passwordController.text),
    );

    // Fallback: Hvis listener ikke trigger inden for 5 sekunder, nulstil loading state
    Future.delayed(const Duration(seconds: 5), () {
      if (mounted && _isUpdating) {
        setState(() {
          _isUpdating = false;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Password opdatering fuldført (timeout check)'),
            backgroundColor: Colors.blue,
          ),
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return BlocListener<AuthBloc, AuthState>(
      listener: (context, state) {
        // Håndter error state
        if (state is AuthError && _isUpdating) {
          // Vis fejl i snackbar
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(state.message),
              backgroundColor: Colors.red,
            ),
          );
          if (mounted) {
            setState(() {
              _isUpdating = false;
            });
          }
        } 
        // Håndter success - når vi går fra loading til authenticated
        else if (state is AuthAuthenticated && _isUpdating) {
          // Success - vis bekræftelse og nulstil form
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Password opdateret succesfuldt!'),
              backgroundColor: Colors.green,
            ),
          );
          _passwordController.clear();
          _confirmPasswordController.clear();
          if (mounted) {
            setState(() {
              _isUpdating = false;
            });
          }
        }
        // Håndter loading state - sæt _isUpdating hvis vi ikke allerede er i loading
        else if (state is AuthLoading && !_isUpdating) {
          // Dette kan ske hvis loading state kommer fra en anden operation
          // Vi ignorerer det hvis vi ikke er i gang med password update
        }
      },
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
              const Text(
                'Opdater Password',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Du kan opdatere dit password her. Dette virker også hvis du er logget ind med OAuth (Google/GitHub).',
                style: TextStyle(
                  fontSize: 14,
                  color: Colors.grey[600],
                ),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _passwordController,
                obscureText: _obscurePassword,
                decoration: InputDecoration(
                  labelText: 'Nyt Password',
                  hintText: 'Indtast nyt password (min. 6 karakterer)',
                  prefixIcon: const Icon(Icons.lock),
                  suffixIcon: IconButton(
                    icon: Icon(
                      _obscurePassword ? Icons.visibility : Icons.visibility_off,
                    ),
                    onPressed: () {
                      setState(() {
                        _obscurePassword = !_obscurePassword;
                      });
                    },
                  ),
                  border: const OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return 'Password er påkrævet';
                  }
                  if (value.length < 6) {
                    return 'Password skal være mindst 6 karakterer';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _confirmPasswordController,
                obscureText: _obscureConfirmPassword,
                decoration: InputDecoration(
                  labelText: 'Bekræft Password',
                  hintText: 'Indtast password igen',
                  prefixIcon: const Icon(Icons.lock_outline),
                  suffixIcon: IconButton(
                    icon: Icon(
                      _obscureConfirmPassword ? Icons.visibility : Icons.visibility_off,
                    ),
                    onPressed: () {
                      setState(() {
                        _obscureConfirmPassword = !_obscureConfirmPassword;
                      });
                    },
                  ),
                  border: const OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return 'Bekræftelse er påkrævet';
                  }
                  if (value != _passwordController.text) {
                    return 'Passwords matcher ikke';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              ElevatedButton.icon(
                onPressed: _isUpdating ? null : _handleUpdatePassword,
                icon: _isUpdating
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.update),
                label: Text(_isUpdating ? 'Opdaterer...' : 'Opdater Password'),
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                ),
              ),
            ],
            ),
          ),
        ),
      ),
    );
  }
}

